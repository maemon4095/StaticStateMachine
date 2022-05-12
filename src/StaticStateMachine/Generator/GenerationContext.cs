using Microsoft.CodeAnalysis;
namespace StaticStateMachine.Generator;

class GenerationContext
{
    public ITypeSymbol Symbol { get; init; }
    public string? ArgType { get; init; }
    public string AssociatedType { get; init; }
    public string StateType { get; init; }
    public string? ContainingNamespace { get; init; }
    public string? InheritedInterfaces { get; init; }
    public string InternalStateVariable { get; init; }
    public string StateVariable { get; init; }
    public string TransitionArgCharaVariable { get; init; }
    public Automaton Automaton { get; init; }
    public Compilation Compilation { get; init; }
}