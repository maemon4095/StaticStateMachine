using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Xunit.Sdk;
using Xunit.Abstractions;
using System.Linq;

namespace StaticStateMachine.Test;

class TestOrderAttributeOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(c => GetOrder(c), CreateComparer());

        static Comparer<int?> CreateComparer()
        {
            return Comparer<int?>.Create((left, right) => (left, right) switch
            {
                (int l, int r) => l.CompareTo(r),
                (int l, null) => 1,
                (null, int r) => -1,
                (null, null) => 0,
            });
        }

        static IAttributeInfo? GetAttributeInfo(ITestCase testCase)
        {
            return testCase.TestMethod.Method.GetCustomAttributes(typeof(TestOrderAttribute)).FirstOrDefault();
        }

        static int? GetOrder(ITestCase testCase)
        {
            return GetAttributeInfo(testCase)?.GetConstructorArguments().FirstOrDefault() as int?;
        }
    }
}