
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

    public static (int nextX, int nextY) FindReachableHuman(GameState gameState)
    {
        if (gameState.Humans == null || gameState.Humans.Where(x => x.IsAlive).Count() == 0)
            return (gameState.Ash.X, gameState.Ash.Y);

        Human nearestHuman = null;

        foreach (var human in gameState.Humans)
        {
            int dx = human.X - gameState.Ash.X;
            int dy = human.Y - gameState.Ash.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            int turnsToReach = (int)((distance / 1000) + 1);
            int firstZombieTurns = int.MaxValue;

            foreach (var zombie in gameState.Zombies)
            {
                int dzx = human.X - zombie.X;
                int dzy = human.Y - zombie.Y;
                double zDistance = Math.Sqrt(dzx * dzx + dzy * dzy);
                int turnsToReachZombies = (int)((zDistance / 400) + 1);
                if (turnsToReachZombies < firstZombieTurns)
                {
                    firstZombieTurns = turnsToReachZombies;
                }
            }

            if (turnsToReach < firstZombieTurns)
            {
                return (human.X, human.Y);
            }
        }

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
    const int SimulationSteps = 4; // Now simulates 5 moves ahead

    static void Main(string[] args)
    {
        string[] inputs;
        int score = 0;
        // game loop
        while (true)
        {
            GameState gameState = ReadGameState(width: 16000, height: 9000, score: score);

            (int X, int Y, int expectedScore) destination = FindBestDestination(gameState, SimulationSteps);

            Console.WriteLine($"{destination.X} {destination.Y}"); // Your destination coordinates
        }
    }

    private static (int X, int Y, int expectedScore) FindBestDestination(GameState gameState, int simulationDepth)
    {
        List<(int nextX, int nextY)> possibleDestinations = GetMovementOptions(gameState.Ash.X, gameState.Ash.Y, gameState.Width, gameState.Height);

        int bestScore = 0;
        (int nextX, int nextY) bestDestination = (gameState.Ash.X, gameState.Ash.Y);

        foreach (var destination in possibleDestinations)
        {
            int score = SimulateMoves(gameState, destination, simulationDepth);
            Console.Error.WriteLine($"Testing destination: {destination.nextX}, {destination.nextY} Score: {score}");
            if (score > bestScore)
            {
                bestScore = score;
                bestDestination = destination;
            }
        }

        if (bestScore <= 0)
        {
            // No valid destination found, move to nearest human or stay put
            bestDestination = Human.FindReachableHuman(gameState);
            bestScore = 0;
        }

        return (bestDestination.nextX, bestDestination.nextY, bestScore);
    }

    // Recursive simulation for multi-step lookahead
    private static int SimulateMoves(GameState gameState, (int nextX, int nextY) destination, int depth)
    {
        // Clone the game state
        GameState clonedState = CloneGameState(gameState);


        // Move Zombies towards nearest human or Ash
        foreach (var zombie in clonedState.Zombies)
        {
            // Find nearest target (human or Ash)
            Human nearestHuman = null;
            double minDist = double.MaxValue;
            foreach (var human in clonedState.Humans)
            {
                double dist = Math.Sqrt((human.X - zombie.X) * (human.X - zombie.X) + (human.Y - zombie.Y) * (human.Y - zombie.Y));
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestHuman = human;
                }
            }
            double distToAsh = Math.Sqrt((clonedState.Ash.X - zombie.X) * (clonedState.Ash.X - zombie.X) + (clonedState.Ash.Y - zombie.Y) * (clonedState.Ash.Y - zombie.Y));
            if (distToAsh < minDist)
            {
                nearestHuman = null; // Target Ash
            }

            int targetX = nearestHuman != null ? nearestHuman.X : clonedState.Ash.X;
            int targetY = nearestHuman != null ? nearestHuman.Y : clonedState.Ash.Y;

            // Move zombie towards target
            double dx = targetX - zombie.X;
            double dy = targetY - zombie.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > 0)
            {
                // Normalize: scale to length 1, then multiply by maxDistance

                double maxDistance = Math.Min(Zombie.SPEED, distance);
                double scale = maxDistance / distance;
                zombie.X += (int)(dx * scale);
                zombie.Y += (int)(dy * scale);
            }
        }

        // Move Ash
        clonedState.Ash.X = destination.nextX;
        clonedState.Ash.Y = destination.nextY;

        // Kill zombies in range
        int score = 0;
        int humanMultiplier = clonedState.Humans.Where(x => x.IsAlive).Count() * 10;
        int zombiesKilled = 0;
        const int killRange = 2000;
        foreach (var zombie in clonedState.Zombies.ToList())
        {
            int dx = zombie.X - clonedState.Ash.X;
            int dy = zombie.Y - clonedState.Ash.Y;
            int distance = (int)Math.Sqrt(dx * dx + dy * dy);
            if (distance <= killRange)
            {
                zombiesKilled++;
                score += GetFibonacci(zombiesKilled + 2) * humanMultiplier;
                clonedState.Zombies.Remove(zombie);
            }
        }

        // Kill humans caught by zombies
        foreach (var zombie in clonedState.Zombies)
        {
            foreach (var human in clonedState.Humans)
            {
                int dx = zombie.X - human.X;
                int dy = zombie.Y - human.Y;
                int distanceToHuman = (int)Math.Sqrt(dx * dx + dy * dy);
                if (distanceToHuman <= Zombie.SPEED)
                {
                    human.IsAlive = false;
                }
            }
        }
        clonedState.Humans = clonedState.Humans.Where(h => h.IsAlive).ToList();

        // If depth > 1, simulate next move recursively
        if (depth > 1 && clonedState.Humans.Count > 0 && clonedState.Zombies.Count > 0)
        {
            var nextMoves = GetMovementOptions(clonedState.Ash.X, clonedState.Ash.Y, clonedState.Width, clonedState.Height);
            int bestFutureScore = 0;
            foreach (var nextMove in nextMoves)
            {
                int futureScore = SimulateMoves(clonedState, nextMove, depth - 1);
                if (futureScore > bestFutureScore)
                    bestFutureScore = futureScore;
            }
            score += bestFutureScore;
#if SIMUL_INPUT
            if (depth == SimulationSteps)
            {
                simulatedState = clonedState;
            }
#endif
        }

        return score * clonedState.Humans.Count;
    }

    // Helper to clone the game state deeply
    private static GameState CloneGameState(GameState gameState)
    {
        return new GameState(gameState.Width, gameState.Height, gameState.Score)
        {
            Ash = new Player(gameState.Ash.X, gameState.Ash.Y),
            Humans = gameState.Humans.Where(x => x.IsAlive).Select(h => (Human)h.Clone()).ToList(),
            Zombies = gameState.Zombies.Select(z => (Zombie)z.Clone()).ToList()
        };
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

    static GameState? simulatedState = null;
    private static GameState ReadGameState(int width, int height, int score)
    {
        if (simulatedState != null)
            return simulatedState;

        GameState gameState = new GameState(width, height, score)
        {
            Ash = ReadPlayer(),
            Humans = ReadHumans(),
            Zombies = ReadZombies(),
        };
        return gameState;
    }


#if !SIMUL_INPUT
    private static List<Zombie> ReadZombies()
    {
        List<Zombie> zombies = new List<Zombie>();
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
#else

 private static List<Zombie> ReadZombies()
    {
        List < Zombie > zombies = new List<Zombie>();
        zombies.Add(new Zombie(2, 8250, 8999, 8250, 8599)); // Example zombie
    

        return zombies;
    }

    private static List<Human> ReadHumans()
    {
        List<Human> humans = new List<Human>();
       humans.Add(new Human(8250, 4500)); // Example human

        return humans;
    }

    private static Player ReadPlayer()
    {
      
        return new Player(1000, 0);
    }

#endif

    private static List<(int nextX, int nextY)> GetMovementOptions(
      int x, int y, int width, int height)
    {
        List<(int nextX, int nextY)> possibleDestinations = new List<(int nextX, int nextY)>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue; // No movement
                double range = 1000;
                if (dx != 0 && dy != 0)
                {
                    range = (Math.Sqrt(2) * 1000) / 2; // Diagonal movement
                }

                int nextX = x + (int)(dx * range);
                int nextY = y + (int)(dy * range);
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
                    continue; // Out of bounds
                possibleDestinations.Add((nextX, nextY));
            }
        }

        return possibleDestinations;
    }
}