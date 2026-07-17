using System.Reflection;
using System.Xml.Linq;
using InvoiceManagement.Api.Controllers;
using InvoiceManagement.Application.Invoices;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Infrastructure;

namespace InvoiceManagement.ArchitectureTests;

internal static class ArchitectureTestSupport
{
    internal static readonly Assembly ApiAssembly = typeof(InvoicesController).Assembly;
    internal static readonly Assembly ApplicationAssembly = typeof(IInvoiceService).Assembly;
    internal static readonly Assembly DomainAssembly = typeof(Invoice).Assembly;
    internal static readonly Assembly InfrastructureAssembly = typeof(DependencyInjection).Assembly;

    internal static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }
    }

    internal static IEnumerable<Type> ExpandType(Type type)
    {
        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (var nestedType in ExpandType(elementType)) yield return nestedType;
            yield break;
        }

        yield return type;
        if (!type.IsGenericType) yield break;

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nestedType in ExpandType(argument)) yield return nestedType;
        }
    }

    internal static void AssertNoReferences(Assembly source, params Assembly[] forbiddenAssemblies)
    {
        var references = source.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);
        var forbidden = forbiddenAssemblies
            .Select(assembly => assembly.GetName().Name)
            .Where(name => name is not null)
            .Where(references.Contains)
            .ToArray();

        Assert.True(
            forbidden.Length == 0,
            $"{source.GetName().Name} must not reference: {string.Join(", ", forbidden)}.");
    }

    internal static void AssertNoReferencesWithPrefix(Assembly source, params string[] forbiddenPrefixes)
    {
        var forbidden = source.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            forbidden.Length == 0,
            $"{source.GetName().Name} must remain framework-independent but references: {string.Join(", ", forbidden)}.");
    }

    internal static void AssertProjectReferences(string projectPath, params string[] expectedReferences)
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot, projectPath));
        var actualReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Replace('\\', '/'))
            .Select(include => Path.GetFileNameWithoutExtension(include))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = expectedReferences.Order(StringComparer.Ordinal).ToArray();

        Assert.True(
            actualReferences.SequenceEqual(expected, StringComparer.Ordinal),
            $"{projectPath} references [{string.Join(", ", actualReferences)}], expected [{string.Join(", ", expected)}].");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InvoiceManagement.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate InvoiceManagement.sln from the test output directory.");
    }
}
