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
[Association("import", 0)]
[Association("import static", 1)]
[Association("output", 2)]
partial struct A
{

}