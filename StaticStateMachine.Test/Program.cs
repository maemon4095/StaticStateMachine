using StaticStateMachine.Generator;
using StaticStateMachine;
using System.Text;

var statemachine = new A();
var buffer = new StringBuilder();

#if true
while (true)
{
    Console.Clear();
    Console.WriteLine(buffer);
    Console.WriteLine($"accept   : {statemachine.State.Accept}");
    if (statemachine.State.Accept)
        Console.WriteLine($"associated : {statemachine.State.Associated}");
    Console.WriteLine($"terminal : {statemachine.State.IsTerminal}");

    var info = Console.ReadKey(true);
    if (info.Key == ConsoleKey.Enter)
    {
        statemachine = new A();
        buffer.Clear();
        continue;
    }
    buffer.Append(info.KeyChar);
    statemachine.Transition(info.KeyChar);
}
#endif



[StaticStateMachine]
[Association("import", Option.Import)]
[Association("import static", Option.ImportStatic)]
[Association("output", Option.Output)]
partial struct A
{

}

enum Option
{
    Import, ImportStatic, Output
}


namespace Test
{
    partial struct Containing
    {
        partial struct Inner
        {
            [StaticStateMachine]
            [Association("import", 0)]
            partial struct B
            {

            }
        }
    }
}
