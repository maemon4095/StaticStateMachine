using StaticStateMachine.Generator;
using Xunit;

namespace StaticStateMachine.Test;

public class StateMachineTest
{
    public StateMachineTest()
    {
        this.runner = SourceGeneratorRunner.Create<StaticStateMachineGenerator>();
    }

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
        this.runner.Run(source).Validate(result =>
        {
            Assert.True(result.Succeeded);

            result.DriverResult.GeneratedTrees.Length.Is(1);
        });
    }
}