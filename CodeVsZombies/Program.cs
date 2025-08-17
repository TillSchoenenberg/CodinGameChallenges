using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Save humans, destroy zombies!
 **/

//public abstract class MoveableEntity
//{
//    abstract public int SPEED { get; protected set; }

//    public abstract (int X, int Y) Move();
//}

public class Human : ICloneable
{
    public bool IsAlive { get; set; } = true;
    public int X { get; set; }
    public int Y { get; set; }

    public Human(int x, int y)
    {
        X = x;
        Y = y;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
  
    public static (int nextX, int nextY) FindNearestHuman(GameState gameState)
    {
        if (gameState.Humans == null || gameState.Humans.Count == 0)
            return (gameState.Ash.X, gameState.Ash.Y);

        Human nearestHuman = null;
        double minDistance = double.MaxValue;

        foreach (var human in gameState.Humans)
        {
            int dx = human.X - gameState.Ash.X;
            int dy = human.Y - gameState.Ash.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestHuman = human;
            }
        }

        if (nearestHuman != null)
            return (nearestHuman.X, nearestHuman.Y);

        return (gameState.Ash.X, gameState.Ash.Y);
    }
   
}

public class Player : ICloneable
{
    internal const int SPEED = 1000;
    public int X { get; set; }  
    public int Y { get; set; }

    public Player(int x, int y)
    {
        X = x;
        Y = y;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

}

public class Zombie : ICloneable
{
    public const int SPEED = 400;
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int NextX { get; set; }
    public int NextY { get; set; }
    public Zombie(int id, int x, int y, int nextX, int nextY)
    {
        Id = id;
        X = x;
        Y = y;
        NextX = nextX;
        NextY = nextY;
    }
    public object Clone()
    {
        return MemberwiseClone();
    }
}

    public class GameState(int width, int height, int score)
    {
        public required Player Ash;
        public required List<Zombie> Zombies;
        public required List<Human> Humans;

        public int Width { get; } = width;
        public int Height { get; } = height;
        public int Score { get; } = score;
}

class Bot
{
    const int SimulationSteps = 10;
    static void Main(string[] args)
    {
        string[] inputs;
        int score = 0;
        // game loop
        while (true)
        {
            GameState gameState = ReadGameState(width: 16000, height: 9000, score: score);

            (int X, int Y, int expectedScore) destination = FindBestDestination(gameState, 1);

            Console.WriteLine($"{destination.X} {destination.Y}"); // Your destination coordinates
        }
    }

    private static (int X, int Y, int expectedScore) FindBestDestination(GameState gameState, int simulationDepth)
    {
        List<(int nextX, int nextY)> possibleDestinations = GetMovementOptions(gameState.Ash.X, gameState.Ash.Y, gameState.Width, gameState.Height);

        int bestScore = -1;
        (int nextX, int nextY) bestDestination = (gameState.Ash.X, gameState.Ash.Y);

        foreach (var destination in possibleDestinations)
        {
            int score = CalculateScore(destination, gameState);
            Console.Error.WriteLine($"Testing destination: {destination.nextX}, {destination.nextY} Score: {score}");
            if(score > bestScore)
            {
                bestScore = score;
                bestDestination = destination;
            }
        }

        if(bestScore <= 0)
        {
            // No valid destination found, move to nearest human or stay put
            bestDestination = Human.FindNearestHuman(gameState);
            bestScore = 0;
        }

        return (bestDestination.nextX, bestDestination.nextY, bestScore);
    }

    private static int CalculateScore((int nextX, int nextY) destination, GameState gameState)
    {
        int score = 0;

        var tmpHumans = KillHumans(gameState);
        if(tmpHumans.Count == 0)
            return 0; // No humans left, no score

        List<Zombie> tmpZombies = gameState.Zombies.Select(z => (Zombie)z.Clone()).ToList();

        score = KillZombies(destination, tmpZombies, tmpHumans);

        return score;
    }

    private static int KillZombies((int nextX, int nextY) destination, List<Zombie> tmpZombies, List<Human> tmpHumans)
    {
        int score = 0;
        int humanMultiplier = tmpHumans.Count * 10;
        int zombiesKilled = 0;

        // Calculate how many zombies are in range of Ash's destination (assume Ash can kill zombies within 2000 units)
        const int killRange = 2000;
        foreach (var zombie in tmpZombies)
        {
            int dx = zombie.X - destination.nextX;
            int dy = zombie.Y - destination.nextY;
            int distance = (int)Math.Sqrt(dx * dx + dy * dy);
            if (distance <= killRange)
            {
                zombiesKilled++;
                score += GetFibonacci(zombiesKilled + 2) * humanMultiplier; // Fibonacci score based on number of zombies killed
            }
        }

        return score;
    }

    private static int GetFibonacci(int n)
    {
        if (n <= 1)
            return n;
        return GetFibonacci(n - 1) + GetFibonacci(n - 2);
    }   

    private static List<Human> KillHumans(GameState gameState)
    {
        var tmpHumans = gameState.Humans.Select(h => (Human)h.Clone()).ToList();

        foreach (var zombie in gameState.Zombies)
        {
            foreach (var human in tmpHumans)
            {
                // Calculate distance from zombie to human
                int dx = zombie.X - human.X;
                int dy = zombie.Y - human.Y;
                int distanceToHuman = (int)Math.Sqrt(dx * dx + dy * dy);
                // If a zombie is close enough to a human, the human is caught
                if (distanceToHuman <= Zombie.SPEED)
                {
                    human.IsAlive = false; // Human is caught by the zombie
                }
            }
        }

        return tmpHumans.Where(h => h.IsAlive).ToList();
    }

    private static GameState ReadGameState(int width, int height, int score)
    {
        GameState gameState = new GameState(width, height, score)
        {
            Ash = ReadPlayer(),
            Humans = ReadHumans(),
            Zombies = ReadZombies(),
        };
        return gameState;
    }

    private static List<Zombie> ReadZombies()
    {
        List < Zombie > zombies = new List<Zombie>();
        int zombieCount = int.Parse(Console.ReadLine());
        for (int i = 0; i < zombieCount; i++)
        {
            var inputs = Console.ReadLine().Split(' ');
            int zombieId = int.Parse(inputs[0]);
            int zombieX = int.Parse(inputs[1]);
            int zombieY = int.Parse(inputs[2]);
            int zombieXNext = int.Parse(inputs[3]);
            int zombieYNext = int.Parse(inputs[4]);
            zombies.Add(new Zombie(zombieId, zombieX, zombieY, zombieXNext, zombieYNext));
        }

        return zombies;
    }

    private static List<Human> ReadHumans()
    {
        List<Human> humans = new List<Human>();
        int humanCount = int.Parse(Console.ReadLine());
        for (int i = 0; i < humanCount; i++)
        {
            var inputs = Console.ReadLine().Split(' ');
            int humanId = int.Parse(inputs[0]);
            int humanX = int.Parse(inputs[1]);
            int humanY = int.Parse(inputs[2]);

            humans.Add(new Human(humanX, humanY));
        }

        return humans;
    }

    private static Player ReadPlayer()
    {
        var inputs = Console.ReadLine().Split(' ');
        int x = int.Parse(inputs[0]);
        int y = int.Parse(inputs[1]);
        return new Player(x, y);
    }


    private static List<(int nextX, int nextY)> GetMovementOptions(
      int x, int y, int width, int height)
    {
        List < (int nextX, int nextY) > possibleDestinations = new List<(int nextX, int nextY)>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for(int dy = -1; dy <= 1; dy++)
            {
                double range = 1000;
                if(dx != 0 && dy != 0)
                {
                    range = Math.Sqrt(2) * 1000; // Diagonal movement
                }

                int nextX = x + (int) (dx * range);
                int nextY = y + (int) (dy * range);
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
                    continue; // Out of bounds
                possibleDestinations.Add((nextX, nextY));
            }
        }

        return possibleDestinations;
    }
}