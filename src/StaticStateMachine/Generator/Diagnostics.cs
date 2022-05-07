using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace StaticStateMachine.Generator;
internal static class Diagnostics
{
    public static DiagnosticDescriptor GenerationErrorDescriptor { get; } = new (
        id:"SSM000", 
        title: "Static State Machine Generation Error",
        messageFormat: "{0} occured static state machine generation. Message : {1}",
        category: "Generation Error",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidAssociationDescriptor { get; } = new(
        id: "SSM001",
        title: "Static State Machine Invalid Association Error",
        messageFormat: "Invalid Association. {0}",
        category: "Association Error",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static Diagnostic GenerationError(Location? location, Exception exception)
    {
        return Diagnostic.Create(GenerationErrorDescriptor, location, exception.GetType(), exception.Message);
    }
    public static Diagnostic InvalidAssociation(InvalidAssociationException ex)
    {
        return Diagnostic.Create(InvalidAssociationDescriptor, ex.Location, ex.Message);
    }

    public static Diagnostic From(Exception exception)
    {
        return exception switch
        {
            InvalidAssociationException ex => InvalidAssociation(ex),
            GeneratorException ex => GenerationError(ex.Location, ex.InnerException),
            _ => GenerationError(Location.None, exception),
        };
    }
}
