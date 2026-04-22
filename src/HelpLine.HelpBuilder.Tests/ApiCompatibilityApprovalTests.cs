using System.CommandLine.Help;
using VerifyXunit;
using Xunit;

namespace HelpLine.HelpBuilderTests;

public class ApiCompatibilityApprovalTests
{
    [Fact]
    public Task HelpLine_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContractForAssembly(typeof(HelpBuilder).Assembly);
        return Verify(contract);
    }
}
