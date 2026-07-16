namespace InvoiceManagement.Domain.Platform;

public sealed class IdempotencyRequest
{
    private IdempotencyRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Operation { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public byte[] RequestHash { get; private set; } = [];

    public IdempotencyState State { get; private set; }

    public Guid? ResourceId { get; private set; }

    public short? ResponseStatus { get; private set; }

    public string? ResponseBody { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTime CreatedUtc { get; private set; }

    public DateTime? CompletedUtc { get; private set; }

    public DateTime ExpiresUtc { get; private set; }
}
