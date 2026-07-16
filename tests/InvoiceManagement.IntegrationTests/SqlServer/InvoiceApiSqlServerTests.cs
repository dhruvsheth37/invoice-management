using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceManagement.Application.Invoices;
using InvoiceManagement.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.IntegrationTests.SqlServer;

[Collection(SqlServerTestGroup.Name)]
public sealed class InvoiceApiSqlServerTests(SqlServerFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [SqlServerFact]
    public async Task Create_replay_search_and_tenant_isolation_work_through_http()
    {
        var tenant = await fixture.SeedTenantAsync("api-a");
        var otherTenant = await fixture.SeedTenantAsync("api-b");
        using var factory = fixture.CreateApi();
        using var client = factory.CreateClient();
        var request = CreateRequest(tenant);

        using var firstResponse = await SendJsonAsync(client, HttpMethod.Post, "/api/v1/invoices", tenant.TenantId, "create-1", request);
        using var replayResponse = await SendJsonAsync(client, HttpMethod.Post, "/api/v1/invoices", tenant.TenantId, "create-1", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        var first = await ReadInvoiceAsync(firstResponse);
        var replay = await ReadInvoiceAsync(replayResponse);
        Assert.Equal(first.Id, replay.Id);

        using var searchResponse = await SendJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/invoices/search",
            tenant.TenantId,
            null,
            new InvoiceListQuery());
        searchResponse.EnsureSuccessStatusCode();
        var page = await searchResponse.Content.ReadFromJsonAsync<CursorResult<InvoiceListItemDto>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(first.Id, page.Items[0].Id);

        using var dashboardResponse = await SendJsonAsync(
            client,
            HttpMethod.Get,
            "/api/v1/dashboard/invoice-summary?asOf=2026-08-20",
            tenant.TenantId,
            null,
            null);
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<InvoiceDashboardDto>(JsonOptions);
        Assert.NotNull(dashboard);
        var currency = Assert.Single(dashboard.CurrencyBreakdown);
        Assert.Equal("USD", currency.CurrencyCode);
        Assert.Equal(1, currency.Draft.Count);
        Assert.Equal(295m, currency.Draft.Amount);

        using var otherTenantResponse = await SendJsonAsync(
            client,
            HttpMethod.Get,
            $"/api/v1/invoices/{first.Id}",
            otherTenant.TenantId,
            null,
            null);
        Assert.Equal(HttpStatusCode.NotFound, otherTenantResponse.StatusCode);

        await using var context = fixture.CreateContext(tenant.TenantId);
        Assert.Equal(1, await context.Invoices.CountAsync());
        Assert.Equal(1, await context.IdempotencyRequests.CountAsync());
    }

    [SqlServerFact]
    public async Task Lifecycle_enforces_etags_and_allocates_monotonic_numbers()
    {
        var tenant = await fixture.SeedTenantAsync("lifecycle");
        using var factory = fixture.CreateApi();
        using var client = factory.CreateClient();
        var first = await CreateInvoiceAsync(client, tenant, "create-first");
        var second = await CreateInvoiceAsync(client, tenant, "create-second");

        using var firstPageResponse = await SendJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/invoices/search",
            tenant.TenantId,
            null,
            new InvoiceListQuery(PageSize: 1, IncludeTotalCount: true));
        firstPageResponse.EnsureSuccessStatusCode();
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<CursorResult<InvoiceListItemDto>>(JsonOptions);
        Assert.NotNull(firstPage);
        Assert.Single(firstPage.Items);
        Assert.Equal(2, firstPage.TotalCount);
        Assert.NotNull(firstPage.ContinuationToken);

        using var secondPageResponse = await SendJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/invoices/search",
            tenant.TenantId,
            null,
            new InvoiceListQuery(PageSize: 1, ContinuationToken: firstPage.ContinuationToken));
        secondPageResponse.EnsureSuccessStatusCode();
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<CursorResult<InvoiceListItemDto>>(JsonOptions);
        Assert.NotNull(secondPage);
        Assert.Single(secondPage.Items);
        Assert.NotEqual(firstPage.Items[0].Id, secondPage.Items[0].Id);
        Assert.Null(secondPage.TotalCount);
        Assert.Null(secondPage.ContinuationToken);

        var firstIssueTask = SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/invoices/{first.Id}/issue",
            tenant.TenantId,
            "issue-first",
            new IssueInvoiceRequest(new DateOnly(2026, 7, 16), new DateOnly(2026, 8, 15)),
            first.ETag);
        var secondIssueTask = SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/invoices/{second.Id}/issue",
            tenant.TenantId,
            "issue-second",
            new IssueInvoiceRequest(new DateOnly(2026, 7, 16), new DateOnly(2026, 8, 15)),
            second.ETag);
        await Task.WhenAll(firstIssueTask, secondIssueTask);

        using var firstIssueResponse = await firstIssueTask;
        using var secondIssueResponse = await secondIssueTask;
        firstIssueResponse.EnsureSuccessStatusCode();
        secondIssueResponse.EnsureSuccessStatusCode();
        var firstIssued = await ReadInvoiceAsync(firstIssueResponse);
        var secondIssued = await ReadInvoiceAsync(secondIssueResponse);

        using var staleResponse = await SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/invoices/{first.Id}/void",
            tenant.TenantId,
            "void-stale",
            new VoidInvoiceRequest("stale request"),
            first.ETag);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        Assert.Equal(
            ["INV-2026-000001", "INV-2026-000002"],
            new[] { firstIssued.InvoiceNumber, secondIssued.InvoiceNumber }.Order(StringComparer.Ordinal));
        Assert.Equal(InvoiceStatus.Issued, firstIssued.Status);
        Assert.NotEqual(first.ETag, firstIssued.ETag);
    }

    [SqlServerFact]
    public async Task Invalid_create_returns_problem_details_without_persisting_invoice()
    {
        var tenant = await fixture.SeedTenantAsync("validation");
        using var factory = fixture.CreateApi();
        using var client = factory.CreateClient();
        var invalid = new CreateInvoiceRequest(
            tenant.CustomerId,
            tenant.LocationId,
            "USD",
            null,
            null,
            [new CreateInvoiceLineRequest("Invalid", 0, 100, 0.18m)]);

        using var response = await SendJsonAsync(client, HttpMethod.Post, "/api/v1/invoices", tenant.TenantId, "invalid-create", invalid);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invoice.line_invalid", problem.GetProperty("errorCode").GetString());
        Assert.True(problem.TryGetProperty("correlationId", out _));
        await using var context = fixture.CreateContext(tenant.TenantId);
        Assert.False(await context.Invoices.AnyAsync());
    }

    private static async Task<InvoiceDto> CreateInvoiceAsync(HttpClient client, TenantData tenant, string idempotencyKey)
    {
        using var response = await SendJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/invoices",
            tenant.TenantId,
            idempotencyKey,
            CreateRequest(tenant));
        response.EnsureSuccessStatusCode();
        return await ReadInvoiceAsync(response);
    }

    private static CreateInvoiceRequest CreateRequest(TenantData tenant) => new(
        tenant.CustomerId,
        tenant.LocationId,
        "USD",
        new DateOnly(2026, 8, 15),
        "Integration test",
        [new CreateInvoiceLineRequest("Freight", 2, 125, 0.18m)]);

    private static async Task<InvoiceDto> ReadInvoiceAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<InvoiceDto>(JsonOptions)
        ?? throw new InvalidOperationException("The API returned an empty invoice response.");

    private static Task<HttpResponseMessage> SendJsonAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        Guid tenantId,
        string? idempotencyKey,
        object? body,
        string? etag = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add("X-Development-User-Id", "101");
        request.Headers.Add("X-Correlation-ID", $"test-{Guid.NewGuid():N}");
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        }
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }
        return client.SendAsync(request);
    }
}
