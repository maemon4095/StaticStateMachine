using SourceGeneratorSupplement;
using Microsoft.CodeAnalysis;
namespace StaticStateMachine.Generator;

partial class StaticStateMachineGenerator
{
    static void WriteDeclaration(IndentedWriter writer, GenerationContext context)
    {
        writer.DeclarationScope(context.Symbol.ContainingSymbol, (writer, symbol) =>
        {
            writer["partial "][context.Symbol.ToDisplayString(Format.TypeDecl)].End();
            if (context.InheritedInterfaces is not null)
            {
                writer[" : "][context.InheritedInterfaces].End();
            }

            writer.Line().BlockScope(1, writer =>
            {
                writer["private int "][context.InternalStateVariable][';'].Line()
                      ["public "][context.StateType][' '][context.StateVariable][" { get; private set; }"].Line();

                WriteReset(writer, context);
                WriteTransition(writer, context);
            });
        });
    }
    static void WriteReset(IndentedWriter writer, GenerationContext context)
    {
        var automaton = context.Automaton;
        writer["public void Reset()"].Line()
              .BlockScope(1, writer =>
              {
                  writer["this."][context.InternalStateVariable][" = "][automaton.InitialState][';'].Line()
                        ["this."][context.StateVariable][" = new "][context.StateType]['('][automaton.IsTerminal(automaton.InitialState) ? "true" : "false"].End();
                  var associated = automaton.InitialStateAssociated;
                  if (associated is not null)
                  {
                      writer[", "][associated].End();
                  }
                  writer[");"].Line();
              });
    }
    static void WriteTransition(IndentedWriter writer, GenerationContext context)
    {
        foreach (var (argType, connections) in GetConnections(context))
        {
            Core(writer, argType, context, connections);
        }

        static IEnumerable<(string, IEnumerable<(int State, IEnumerable<(Automaton.Arg Arg, int Dst, string? Associated)>)>)> GetConnections(GenerationContext context)
        {
            var automaton = context.Automaton;
            if (context.ArgType is not null) return Enumerable.Repeat((context.ArgType, automaton.EnumerateConnections()), 1);

            return automaton.EnumerateFlattenConnections().GroupBy(c => c.Arg.Type, SymbolEqualityComparer.Default).Select(g =>
            {
                var argType = g.Key!.ToDisplayString(Format.GlobalFullName);
                return (argType, g.GroupBy(c => c.State).Select(g => (g.Key, g.Select(p => (p.Arg, p.Dst, p.Associated)))));
            });
        }

        static void Core(IndentedWriter writer, string argType, GenerationContext context, IEnumerable<(int, IEnumerable<(Automaton.Arg, int, string?)>)> connections)
        {
            writer["public bool Transition("][argType][' '][context.TransitionArgCharaVariable][')'].Line()
                  ['{'].Line().Indent(1)
                  ["switch(this."][context.InternalStateVariable][')'].Line()
                  ['{'].Line().Indent(1);

            foreach (var (state, transitions) in connections)
            {
                writer["case "][state][':'].Line().Indent(1)
                      ["switch("][context.TransitionArgCharaVariable][')'].Line()
                      ['{'].Line().Indent(1);

                var containsWildCard = false;
                foreach (var (arg, dst, associated) in transitions)
                {
                    if (arg.IsWildCard)
                    {
                        containsWildCard = true;
                        writer["default :"].Line();
                    }
                    else
                    {
                        writer["case "][arg.Literal][':'].Line();
                    }
                    writer.Indent(1)
                          ["this."][context.InternalStateVariable][" = "][dst][';'].Line()
                          ["this."][context.StateVariable][" = new "][context.StateType]['('][dst < 0 ? "true" : "false"].End();
                    if (associated is not null)
                    {
                        writer[", "][associated].End();
                    }
                    writer[");"].Line();
                    writer["return "][dst < 0 ? "false" : "true"][';'].Line().Indent(-1);
                }
                if (!containsWildCard)
                {
                    writer["default:"].Line().Indent(1)
                          ["this."][context.InternalStateVariable][" = -1;"].Line()
                          ["this."][context.StateVariable][" = new "][context.StateType]["(true);"].Line()
                          ["return false;"].Line().Indent(-1);
                }
                writer.Indent(-1)
                      ['}'].Line().Indent(-1);
            }

            writer["default: return false;"].Line().Indent(-1)
                  ['}'].Line().Indent(-1)
                  ['}'].Line();
        }
    }
}