using StaticStateMachine.Generator;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using SourceGeneratorRunner;
using SourceGeneratorRunner.Testing;

namespace StaticStateMachine.Test;

public class StateMachineTest
{
    public StateMachineTest(ITestOutputHelper helper)
    {
        this.helper = helper;
        this.runner = GeneratorRunner.Create(() => new StaticStateMachineGenerator());
    }

    readonly ITestOutputHelper helper;
    readonly GeneratorRunner runner;

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
            helper.WriteLine(string.Join("\n\n",result.GeneratedSources.Select(source => source.SourceText.ToString())));
            Assert.True(result.Succeeded);
        });
    }
}