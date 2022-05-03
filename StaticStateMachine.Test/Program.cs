using StaticStateMachine.Attributes;
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
[Association("/:", 0)]
[Association(":/", 1)]
[Association("/#", 2)]
[Association("/$", 3)]
[Association("$/", 4)]
[Association("/@", 5)]
[Association("\r", 6)]
[Association("\r\n", 7)]
[Association("\n", 8)]
partial struct A
{

}

[StaticStateMachine]
[Association(new[] { 0L }, 0)]
[Association(new[] { 1L, 2L }, 1)]
[Association(new[] { 0, 1 }, 2)]
partial struct B
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
