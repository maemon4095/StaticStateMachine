using System.Collections.Immutable;
using Xunit;

using StaticStateMachine.Generator;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace StaticStateMachine.Test;

public class StateMachineTest
{
    public StateMachineTest(ITestOutputHelper helper)
    {
        this.helper = helper;
        this.runner = SourceGeneratorRunner.Create<StaticStateMachineGenerator>(SourceGeneratorRunner.Config.Default);
    }

    readonly SourceGeneratorRunner runner;
    readonly ITestOutputHelper helper;

    [Fact]
    public void EmptyAssociation()
    {
        var source = @"
using StaticStateMachine;
[StaticStateMachine]
partial struct A
{
}";
        this.runner.Run(source).Validate(result =>
        {
            if (result.Succeeded)
            {

            }
            else
            {
                this.helper.WriteLine(string.Join("\n\n", result.Compilation.GetDiagnostics().Select(d => d.GetMessage())));
                this.helper.WriteLine(result.Exception?.Message);
            }
            Assert.True(result.Succeeded);
        });
    }
}