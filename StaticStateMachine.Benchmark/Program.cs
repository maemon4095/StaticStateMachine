using BenchmarkDotNet.Running;
using StaticStateMachine.Generator;
using StaticStateMachine;
using BenchmarkDotNet.Attributes;

var summary = BenchmarkRunner.Run<StateMachineBenchmark>();


public class StateMachineBenchmark
{
    private SampleStateMachine stateMachine;

    [IterationSetup]
    public void Setup()
    {
        this.stateMachine = new SampleStateMachine();
    }

    [Benchmark]
    [ArgumentsSource(nameof(StringArgs))]
    public MachineState<int> Transition(string str)
    {
        var stateMachine = this.stateMachine;
        foreach(var chara in str)
        {
            if(!stateMachine.Transition(chara)) break;
        }
        return stateMachine.State;
    }

    [Benchmark]
    [Arguments('a')]
    public MachineState<int> Transition(char chara)
    {
        this.stateMachine.Transition(chara);
        return this.stateMachine.State;
    }

    public IEnumerable<object[]> StringArgs()
    {
        yield return new[] { "abc" };
        yield return new[] { "abcde" };
        yield return new[] { "qwlkxMgeKd]exbNVrpYG" };
        yield return new[] { "v" };
    }
}

[StaticStateMachine]
[Association("abc", 0)]
[Association("abcde", 1)]
[Association("qwlkxMgeKd]exbNVrpYG", 2)]
partial struct SampleStateMachine
{

}