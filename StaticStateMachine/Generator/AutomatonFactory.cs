using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace StaticStateMachine.Generator;

internal static class AutomatonFactory
{
    public static Automaton Create(StateMachineCategory category, ImmutableArray<(TypedConstant Pattern, TypedConstant Associated)> associations)
    {
        switch (category)
        {
            case StateMachineCategory.PlainText:
            {
                var a = associations.Select(a => (
                    a.Pattern.Kind switch
                    {
                        TypedConstantKind.Array => a.Pattern.Values.Select(LiteralFactory.From).ToImmutableArray(),
                        _ => (a.Pattern.Value as string)?.Select(c => LiteralFactory.From(c)).ToImmutableArray() ?? ImmutableArray<string>.Empty,
                    },
                    LiteralFactory.From(a.Associated)
                ));
                return CreatePlainText(a);
            }
            default: throw new NotSupportedException($"state machine category({category}) is not supported");
        }
    }

    public static Automaton CreatePlainText(IEnumerable<(ImmutableArray<string> Pattern, string Associated)> associations)
    {
        var automaton = new Automaton();

        foreach (var (pattern, associated) in associations)
        {
            Add(automaton, pattern, associated);
        }

        return automaton;

        static void Add(Automaton automaton, ImmutableArray<string> pattern, string associated)
        {
            if (pattern.Length == 0) return;
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