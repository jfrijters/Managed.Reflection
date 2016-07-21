using System;
using Xunit;
using Managed.Reflection;

namespace Tests
{
    public class Tests
    {
        [Fact]
        public void Test1() 
        {
            var universe = new Universe();
            var typeofTests = universe.Import(typeof(Tests));
            Assert.Equal(typeof(Tests).FullName, typeofTests.FullName);
            Assert.Equal(typeof(Tests).AssemblyQualifiedName, typeofTests.AssemblyQualifiedName);
        }
    }
}
