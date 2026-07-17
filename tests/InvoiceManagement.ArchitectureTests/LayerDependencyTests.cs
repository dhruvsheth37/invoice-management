namespace InvoiceManagement.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_does_not_reference_outer_layers_or_frameworks()
    {
        ArchitectureTestSupport.AssertNoReferences(
            ArchitectureTestSupport.DomainAssembly,
            ArchitectureTestSupport.ApplicationAssembly,
            ArchitectureTestSupport.InfrastructureAssembly,
            ArchitectureTestSupport.ApiAssembly);
        ArchitectureTestSupport.AssertNoReferencesWithPrefix(
            ArchitectureTestSupport.DomainAssembly,
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Application_does_not_reference_api_infrastructure_or_web_and_persistence_frameworks()
    {
        ArchitectureTestSupport.AssertNoReferences(
            ArchitectureTestSupport.ApplicationAssembly,
            ArchitectureTestSupport.InfrastructureAssembly,
            ArchitectureTestSupport.ApiAssembly);
        ArchitectureTestSupport.AssertNoReferencesWithPrefix(
            ArchitectureTestSupport.ApplicationAssembly,
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Infrastructure_does_not_reference_api()
    {
        ArchitectureTestSupport.AssertNoReferences(
            ArchitectureTestSupport.InfrastructureAssembly,
            ArchitectureTestSupport.ApiAssembly);
    }
}
