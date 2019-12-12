using System;
using System.Collections.Generic;
using Xunit;

namespace Netspark.CleanArchitecture.UnitTests
{
    public class TestConsole
    {
        [Fact]
        public void StackTest()
        {
            var stack = new Stack<string>();
            stack.Push("1");
            stack.Push("2");

            var result = stack.ToArray();
        }
    }
}
