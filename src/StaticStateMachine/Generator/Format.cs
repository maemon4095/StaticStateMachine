using Microsoft.CodeAnalysis;

namespace StaticStateMachine.Generator;

internal static class Format
{
    public static SymbolDisplayFormat TypeDecl { get; } = new SymbolDisplayFormat(
        kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);
    public static SymbolDisplayFormat GlobalFullName { get; } = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);
    public static SymbolDisplayFormat FileName { get; } = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
}