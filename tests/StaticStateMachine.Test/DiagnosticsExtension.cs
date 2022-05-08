using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections.Generic;

namespace StaticStateMachine.Test;

static class DiagnosticsExtension
{
    public static bool Validate(this IEnumerable<Diagnostic> diagnostics, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        return !diagnostics.Any(d => d.Severity >= severity);
    }
}