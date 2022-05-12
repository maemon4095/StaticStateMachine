using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
namespace StaticStateMachine.Generator;
public partial class StaticStateMachineGenerator
{
    static bool DefaultEquals(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);
    static ITypeSymbol? LowestCommonAncestorOf(IEnumerable<ITypeSymbol?> types)
    {
        var common = default(ITypeSymbol);
        foreach (var type in types)
        {
            if (type is null) continue;
            if (common is null)
            {
                common = type;
                continue;
            }
            var attributeBases = BaseTypes(type);
            if (attributeBases.Contains(common, SymbolEqualityComparer.Default)) continue;
            common = BaseTypes(common).Intersect(attributeBases, SymbolEqualityComparer.Default).First() as ITypeSymbol;
        }
        return common;
        static IEnumerable<ITypeSymbol> BaseTypes(ITypeSymbol? symbol)
        {
            while (symbol is not null)
            {
                yield return symbol;
                symbol = symbol.BaseType;
            }
        }
    }
    static IEnumerable<INamedTypeSymbol> Containings(ITypeSymbol symbol)
    {
        if (symbol.ContainingType is null) yield break;
        foreach (var containing in Containings(symbol.ContainingType)) yield return containing;
        yield return symbol.ContainingType;
    }
}
