using Microsoft.CodeAnalysis;

namespace StaticStateMachine.Generator;

public partial class StaticStateMachineGenerator
{
    class GenerationContext
    {
        public string ArgType { get; init; }
        public string AssociatedType { get; init; }
        public string TypeCategory { get; init; }
        public string TypeIdentifier { get; init; }
        public string? ContainingNamespace { get; init; }
        public string InheritedInterface { get; init; }
        public string StateFullName { get; init; }
        public AttributeData StateMachineAttributeData { get; init; }
        public IEnumerable<AttributeData> AssociationAttributes { get; init; }
    }
}