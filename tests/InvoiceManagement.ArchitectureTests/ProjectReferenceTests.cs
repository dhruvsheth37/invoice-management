namespace InvoiceManagement.ArchitectureTests;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void Domain_has_no_project_references() =>
        ArchitectureTestSupport.AssertProjectReferences(
            "src/InvoiceManagement.Domain/InvoiceManagement.Domain.csproj");

    [Fact]
    public void Application_references_only_domain() =>
        ArchitectureTestSupport.AssertProjectReferences(
            "src/InvoiceManagement.Application/InvoiceManagement.Application.csproj",
            "InvoiceManagement.Domain");

    [Fact]
    public void Infrastructure_references_only_application_and_domain() =>
        ArchitectureTestSupport.AssertProjectReferences(
            "src/InvoiceManagement.Infrastructure/InvoiceManagement.Infrastructure.csproj",
            "InvoiceManagement.Application",
            "InvoiceManagement.Domain");

    [Fact]
    public void Api_is_the_composition_root() =>
        ArchitectureTestSupport.AssertProjectReferences(
            "src/InvoiceManagement.Api/InvoiceManagement.Api.csproj",
            "InvoiceManagement.Application",
            "InvoiceManagement.Infrastructure");
}
