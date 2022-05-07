using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace StaticStateMachine.Generator;

internal static class AutomatonFactory
{
    public static Automaton Create(Compilation compilation, StateMachineCategory category, ImmutableArray<(TypedConstant Pattern, TypedConstant Associated)> associations)
    {
        var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);
        var charSymbol = compilation.GetSpecialType(SpecialType.System_Char);

        switch (category)
        {
            case StateMachineCategory.TypeWise:
            case StateMachineCategory.Plain:
            {
                var a = associations.Select(a => (
                    a.Pattern.Kind switch
                    {
                        TypedConstantKind.Array => a.Pattern.Values.Select(c => new Automaton.Arg(c.Type ?? objectSymbol, LiteralFactory.From(c))).ToImmutableArray(),
                        _ => (a.Pattern.Value as string)?.Select(c => new Automaton.Arg(charSymbol, LiteralFactory.From(c))).ToImmutableArray() ?? ImmutableArray<Automaton.Arg>.Empty,
                    },
                    LiteralFactory.From(a.Associated))
                );
                return CreatePlainOrTypeWise(a);
            }
            default: throw new NotSupportedException($"state machine category({category}) is not supported");
        }
    }

    public static Automaton CreatePlainOrTypeWise(IEnumerable<(ImmutableArray<Automaton.Arg> Pattern, string Associated)> associations)
    {
        var automaton = new Automaton();

        foreach (var (pattern, associated) in associations)
        {
            Add(automaton, pattern, associated);
        }

        return automaton;

        static void Add(Automaton automaton, ImmutableArray<Automaton.Arg> pattern, string associated)
        {
            if (pattern.Length <= 0)
            {
                automaton.Associate(automaton.InitialState, associated);
                return;
            }
            var state = automaton.InitialState;
            var index = 0;
            var body = pattern.AsSpan()[..^1];
            var last = pattern.Last();

            foreach (var chara in body)
            {
                var (next, _) = automaton.Transition(state, chara);
                if (next < 0) break;
                state = next;
                index++;
            }

            while (index < body.Length)
            {
                var chara = body[index];
                var dst = automaton.AddState();
                automaton.Connect(state, chara, dst);
                state = dst;
                index++;
            }

            automaton.Associate(state, last, associated);
        }
    }
}