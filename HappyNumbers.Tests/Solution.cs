using NUnit.Framework.Internal.Execution;

namespace HappyNumbers.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ProcessNumber_HappyNumber()
        {
            string expected = "23 :)";
            string actual = Solution.ProcessNumber("23");
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void ProcessNumber_UnhappyNumber()
        {
            string expected = "24 :(";
            string actual = Solution.ProcessNumber("24");
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}