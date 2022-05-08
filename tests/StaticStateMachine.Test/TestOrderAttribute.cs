using System;

namespace StaticStateMachine.Test;

[AttributeUsage(AttributeTargets.Method)]
class TestOrderAttribute : Attribute
{
    public TestOrderAttribute(int order)
    {
        this.Order = order;
    }

    public int Order { get; }
}
