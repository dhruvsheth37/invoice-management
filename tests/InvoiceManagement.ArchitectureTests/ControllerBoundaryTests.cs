using System.Reflection;

namespace InvoiceManagement.ArchitectureTests;

public sealed class ControllerBoundaryTests
{
    private static readonly IReadOnlyList<Type> ControllerTypes = ArchitectureTestSupport
        .GetLoadableTypes(ArchitectureTestSupport.ApiAssembly)
        .Where(type => type.IsClass && type.IsPublic && type.Namespace == "InvoiceManagement.Api.Controllers")
        .ToArray();

    [Fact]
    public void Controllers_are_sealed_api_controllers_with_explicit_routes()
    {
        var violations = ControllerTypes
            .Where(type => !type.IsSealed ||
                           !HasAttribute(type, "Microsoft.AspNetCore.Mvc.ApiControllerAttribute") ||
                           !HasAttribute(type, "Microsoft.AspNetCore.Mvc.RouteAttribute"))
            .Select(type => type.FullName)
            .ToArray();

        AssertNoViolations(violations, "Controllers must be sealed and declare [ApiController] and [Route]");
    }

    [Fact]
    public void Controllers_do_not_depend_on_domain_entities_or_infrastructure_types()
    {
        var violations = new List<string>();
        foreach (var controller in ControllerTypes)
        {
            foreach (var constructor in controller.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
            {
                AddForbiddenTypes(violations, controller, ".ctor", constructor.GetParameters().Select(parameter => parameter.ParameterType));
            }

            AddForbiddenTypes(
                violations,
                controller,
                "field",
                controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(field => field.FieldType));

            foreach (var method in controller.GetMethods(
                         BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                         BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                AddForbiddenTypes(
                    violations,
                    controller,
                    method.Name,
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .Append(method.ReturnType)
                        .Concat(method.GetMethodBody()?.LocalVariables.Select(local => local.LocalType) ?? []));
            }
        }

        AssertNoViolations(
            violations,
            "Controllers must communicate through Application contracts; Domain entities and Infrastructure types are forbidden");
    }

    [Fact]
    public void Application_dependencies_in_controller_constructors_are_interfaces()
    {
        var violations = ControllerTypes
            .SelectMany(controller => controller.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                .SelectMany(constructor => constructor.GetParameters())
                .Where(parameter => parameter.ParameterType.Assembly == ArchitectureTestSupport.ApplicationAssembly &&
                                    !parameter.ParameterType.IsInterface)
                .Select(parameter => $"{controller.Name} -> {parameter.ParameterType.FullName}"))
            .ToArray();

        AssertNoViolations(violations, "Controllers must depend on Application interfaces, not concrete use-case implementations");
    }

    [Fact]
    public void Public_actions_declare_http_response_and_cancellation_contracts()
    {
        var violations = new List<string>();
        foreach (var controller in ControllerTypes)
        {
            foreach (var action in PublicActions(controller))
            {
                var attributes = action.CustomAttributes.Select(attribute => attribute.AttributeType.FullName).ToArray();
                if (!attributes.Any(IsHttpMethodAttribute))
                    violations.Add($"{controller.Name}.{action.Name} has no HTTP method attribute");
                if (!attributes.Any(IsProducesResponseTypeAttribute))
                    violations.Add($"{controller.Name}.{action.Name} has no ProducesResponseType attribute");
                if (!action.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)))
                    violations.Add($"{controller.Name}.{action.Name} has no CancellationToken parameter");
            }
        }

        AssertNoViolations(
            violations,
            "Public controller actions must make their HTTP method, successful response, and cancellation behavior explicit");
    }

    private static IEnumerable<MethodInfo> PublicActions(Type controller) =>
        controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

    private static void AddForbiddenTypes(
        List<string> violations,
        Type controller,
        string member,
        IEnumerable<Type> signatureTypes)
    {
        var forbiddenTypes = signatureTypes
            .SelectMany(ArchitectureTestSupport.ExpandType)
            .Where(type => type.Assembly == ArchitectureTestSupport.DomainAssembly ||
                           type.Assembly == ArchitectureTestSupport.InfrastructureAssembly)
            .Distinct()
            .Select(type => type.FullName);

        foreach (var forbiddenType in forbiddenTypes)
            violations.Add($"{controller.Name}.{member} -> {forbiddenType}");
    }

    private static bool HasAttribute(MemberInfo member, string attributeType) =>
        member.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == attributeType);

    private static bool IsHttpMethodAttribute(string? attributeType) =>
        attributeType is not null &&
        attributeType.StartsWith("Microsoft.AspNetCore.Mvc.Http", StringComparison.Ordinal) &&
        attributeType.EndsWith("Attribute", StringComparison.Ordinal);

    private static bool IsProducesResponseTypeAttribute(string? attributeType) =>
        attributeType is not null &&
        attributeType.StartsWith("Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute", StringComparison.Ordinal);

    private static void AssertNoViolations(IEnumerable<string?> violations, string rule)
    {
        var materialized = violations.Where(violation => violation is not null).ToArray();
        Assert.True(materialized.Length == 0, $"{rule}:{Environment.NewLine}{string.Join(Environment.NewLine, materialized)}");
    }
}
