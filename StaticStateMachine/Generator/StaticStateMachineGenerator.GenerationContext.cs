using Microsoft.CodeAnalysis;

namespace StaticStateMachine.Generator;

public partial class StaticStateMachineGenerator
{
    class GenerationContext
    {
        public ITypeSymbol Symbol { get; init; }
        public string ArgType { get; init; }
        public string AssociatedType { get; init; }
        public string TypeDecl { get; init; }
        public string? ContainingNamespace { get; init; }
        public string InheritedInterface { get; init; }
        public string StateFullName { get; init; }
        public AttributeData StateMachineAttributeData { get; init; }
        public IEnumerable<AttributeData> AssociationAttributes { get; init; }
    }
}