using NetArchTest.Rules;
using Xunit;

namespace PolicyOperations.Application.Tests;

public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(global::PolicyOperations.Domain.Policies.Policy).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(global::PolicyOperations.Application.Policies.PolicyService).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(global::PolicyOperations.Infrastructure.Persistence.EfPolicyRepository).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(global::PolicyOperations.Api.Controllers.PoliciesController).Assembly;

    [Fact]
    public void DomainMustNotHaveDependencyOnOtherProjects()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "PolicyOperations.Application",
                "PolicyOperations.Infrastructure",
                "PolicyOperations.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain must not depend on Application, Infrastructure, or Api layers.");
    }

    [Fact]
    public void ApplicationMustNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "PolicyOperations.Infrastructure",
                "PolicyOperations.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, "Application must not depend on Infrastructure or Api layers.");
    }

    [Fact]
    public void DomainEntitiesMustNotDependOnEntityFrameworkOrAspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("PolicyOperations.Domain.Policies")
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain entities must remain POCOs without EF Core or ASP.NET Core dependencies.");
    }

    [Fact]
    public void InfrastructureMustNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("PolicyOperations.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, "Infrastructure must not depend on the Api entrypoint layer.");
    }
}
