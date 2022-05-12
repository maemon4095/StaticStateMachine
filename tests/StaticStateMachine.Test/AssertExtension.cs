using Xunit;

namespace StaticStateMachine.Test;

static class AssertExtension
{
    public static void Is<T>(this T value, T expected)
    {
        Assert.Equal(expected, value);
    }
}
