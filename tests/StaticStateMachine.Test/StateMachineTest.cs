using StaticStateMachine.Generator;
using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace StaticStateMachine.Test;

public class StateMachineTest
{
    public StateMachineTest(ITestOutputHelper helper)
    {
        this.helper = helper;
        this.runner = SourceGeneratorRunner.Create<StaticStateMachineGenerator>();
    }

    readonly ITestOutputHelper helper;
    readonly SourceGeneratorRunner runner;

    [Fact]
    public void EmptyAssociation()
    {
        var source = @"
using StaticStateMachine;
[StaticStateMachine]
partial struct A
{
}";
        this.runner.Run(source).Verify(result =>
        {
            var helper = this.helper;
            helper.WriteLine(string.Join("\n\n",result.GeneratorResult.GeneratedSources.Select(source => source.SourceText.ToString())));
            Assert.True(result.Succeeded);
        });
    }
}