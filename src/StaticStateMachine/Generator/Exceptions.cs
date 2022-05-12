using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace StaticStateMachine.Generator;

sealed class GeneratorException : Exception
{
    public GeneratorException(Location? location, Exception inner)
        : base(string.Empty, inner)
    {
        this.Location = location;
    }

    public Location? Location { get; }
}

class InvalidAssociationException : Exception
{
    public static InvalidAssociationException NotSupportedPatternType(Location? location, ITypeSymbol? symbol)
    {
        if (symbol is null) return new(location, $"Association pattern type should be array or string.");
        return new InvalidAssociationException(location, $"Type {symbol.Name} is not supported association pattern type.");
    }

    public InvalidAssociationException(Location? location, string message)
        : base(message)
    {
        this.Location = location;
    }

    public Location? Location { get; }
}