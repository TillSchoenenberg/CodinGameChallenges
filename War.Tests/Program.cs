namespace War.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            int[] player1 = { 10, 9, 8, 13, 7, 5, 6};
            int[] player2 = { 10, 7, 5, 12, 2, 4, 6 };
            int winner = Solution.PlayGame(new Queue<int>(player1), new Queue<int>(player2), out int turns);

            Assert.AreEqual(winner, 1);
            Assert.AreEqual(turns, 2);
        }
    }
}