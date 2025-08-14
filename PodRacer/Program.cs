using System;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/

class BoardState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int NextCheckpointX { get; set; }
    public int NextCheckpointY { get; set; }
    public int NextCheckpointDist { get; set; }
    public int NextCheckpointAngle { get; set; }
    public int OpponentX { get; set; }
    public int OpponentY { get; set; }
}

class Player
{
    /// <summary>
    /// Entry point for the PodRacer game loop.
    /// </summary>
    static void Main(string[] args)
    {
        bool hasUsedBoost = false;
        // game loop
        while (true)
        {
            bool isBoostRecommended = GetInputs(out BoardState gameState);
            int thrust = CalculateThrust(gameState, hasUsedBoost);

            if (hasUsedBoost || !isBoostRecommended)
            {
                Console.WriteLine($"{gameState.NextCheckpointX} {gameState.NextCheckpointY} {thrust}");
            }
            else
            {
                hasUsedBoost = true; // Use boost only once
                Console.WriteLine($"{gameState.NextCheckpointX} {gameState.NextCheckpointY} BOOST");
            }
        }
    }

    /// <summary>
    /// Parses input and updates the game state.
    /// </summary>
    private static bool GetInputs(out BoardState gameState)
    {
        bool isBoostRecommended = false;
        gameState = new BoardState();

        string? inputLine = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(inputLine))
            throw new InvalidOperationException("Input line is null or empty.");

        string[] inputs = inputLine.Split(' ');
        if (inputs.Length < 6)
            throw new FormatException("Insufficient input data for player.");

        gameState.X = int.Parse(inputs[0]);
        gameState.Y = int.Parse(inputs[1]);
        gameState.NextCheckpointX = int.Parse(inputs[2]);
        gameState.NextCheckpointY = int.Parse(inputs[3]);
        int nextCheckpointDist = int.Parse(inputs[4]);
        gameState.NextCheckpointDist = nextCheckpointDist;
        gameState.NextCheckpointAngle = int.Parse(inputs[5]);

        // Recommend boost if at max distance and angle is small
        if (nextCheckpointDist >= 6000 && Math.Abs(gameState.NextCheckpointAngle) < 5)
        {
            isBoostRecommended = true;
        }

        inputLine = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(inputLine))
            throw new InvalidOperationException("Opponent input line is null or empty.");

        inputs = inputLine.Split(' ');
        if (inputs.Length < 2)
            throw new FormatException("Insufficient input data for opponent.");

        gameState.OpponentX = int.Parse(inputs[0]);
        gameState.OpponentY = int.Parse(inputs[1]);

        return isBoostRecommended;
    }

    /// <summary>
    /// Improved thrust calculation:
    /// - Full thrust if angle is small and distance is large.
    /// - Reduce thrust as angle increases or distance decreases.
    /// - Minimum thrust if angle is very large (sharp turn).
    /// </summary>
    private static int CalculateThrust(BoardState gameState, bool hasUsedBoost)
    {
        int angle = Math.Abs(gameState.NextCheckpointAngle);
        int dist = gameState.NextCheckpointDist;

        // If angle is very large, slow down for sharp turn
        if (angle > 120)
            return 20;
        if (angle > 90)
            return 40;

        // If distance is very small, slow down to avoid overshooting
        if (dist < 1000)
            return 30;

        // If distance is moderate, use moderate thrust
        if (dist < 3000)
            return 60;

        // If angle is small and distance is large, use max thrust
        if (angle < 10 && dist > 6000 && !hasUsedBoost)
            return 100;

        // Otherwise, scale thrust based on distance and angle
        double angleFactor = 1.0 - (angle / 90.0); // 1 at 0°, 0 at 90°
        double distFactor = Math.Min(dist / 6000.0, 1.0); // 1 at 6000+, <1 below
        int thrust = (int)(100 * angleFactor * distFactor);

        // Clamp thrust to [30,100] for safety
        thrust = Math.Clamp(thrust, 30, 100);

        return thrust;
    }
}
