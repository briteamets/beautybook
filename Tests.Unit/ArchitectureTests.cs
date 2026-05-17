using System.Reflection;
using App.BLL.Services;
using App.Contracts;
using App.DAL.EF;
using App.Domain;

namespace Tests.Unit;

public class ArchitectureTests
{
    private static IEnumerable<string> ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

    [Fact]
    public void DalEf_DoesNotReference_Bll()
    {
        var dal = typeof(AppDbContext).Assembly;
        Assert.DoesNotContain("App.BLL", ReferencedAssemblyNames(dal));
    }

    [Fact]
    public void Bll_DoesNotReference_DalEf()
    {
        var bll = typeof(SlotGeneratorService).Assembly;
        Assert.DoesNotContain("App.DAL.EF", ReferencedAssemblyNames(bll));
    }

    [Fact]
    public void Contracts_DoesNotReference_BllOrDalEf()
    {
        var contracts = typeof(IAppUnitOfWork).Assembly;
        var refs = ReferencedAssemblyNames(contracts).ToList();
        Assert.DoesNotContain("App.BLL", refs);
        Assert.DoesNotContain("App.DAL.EF", refs);
    }
}
