using System;

class Player
{
    // Persistent state across turns
    static bool boostUsed = false;
    static int prevX = int.MinValue, prevY = int.MinValue;
    static int prevOppX = int.MinValue, prevOppY = int.MinValue;

    static void Main()
    {
        while (true)
        {
            // Read own pod state
            string[] inputs = Console.ReadLine().Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            int nextCheckpointX = int.Parse(inputs[2]);
            int nextCheckpointY = int.Parse(inputs[3]);
            int nextCheckpointDist = int.Parse(inputs[4]);
            int nextCheckpointAngle = int.Parse(inputs[5]);

            // Read opponent (single pod)
            inputs = Console.ReadLine().Split(' ');
            int opponentX = int.Parse(inputs[0]);
            int opponentY = int.Parse(inputs[1]);

            // Compute velocity (approx from last frame)
            int vx = 0, vy = 0;
            if (prevX != int.MinValue)
            {
                vx = x - prevX;
                vy = y - prevY;
            }
            double speed = Hypot(vx, vy);

            // Opponent velocity and relative speed (for SHIELD)
            int oppVx = 0, oppVy = 0;
            if (prevOppX != int.MinValue)
            {
                oppVx = opponentX - prevOppX;
                oppVy = opponentY - prevOppY;
            }
            double relV = Hypot(vx - oppVx, vy - oppVy);
            double oppDist = Hypot(opponentX - x, opponentY - y);

            // Inertia-compensated aim (lead against our drift)
            // Aim "inside" the turn by offsetting opposite to current velocity
            // Factor tuned to ~3 frames of drift compensation
            int leadFactor = 3;
            int targetX = nextCheckpointX - vx * leadFactor;
            int targetY = nextCheckpointY - vy * leadFactor;

            // Optional gentle clamp to map bounds to avoid extreme targets
            targetX = Clamp(targetX, -1000, 17000); // a little leeway is fine
            targetY = Clamp(targetY, -1000, 10000);

            // Thrust control
            int thrust = 100;
            int absAngle = Math.Abs(nextCheckpointAngle);

            // Hard braking if we're pointing away
            if (absAngle > 90) thrust = 0;
            else if (absAngle > 70) thrust = 20;

            // Distance-based braking: closer checkpoints -> ease off
            // Brake distance scales with speed (heuristic)
            int brakeDist = (int)Math.Max(1200, speed * 6.0);
            if (nextCheckpointDist < brakeDist)
            {
                // Blend: more braking if angle is still significant
                if (absAngle > 45) thrust = Math.Min(thrust, 30);
                else thrust = Math.Min(thrust, 55);
            }

            // BOOST on a perfect long straight once
            string action = thrust.ToString();
            if (!boostUsed && absAngle < 5 && nextCheckpointDist > 6000 && speed > 250)
            {
                action = "BOOST";
                boostUsed = true;
            }
            else
            {
                // SHIELD if we're about to collide (close + high relative speed)
                if (oppDist < 800 && relV > 300 && speed > 200 && absAngle < 45)
                {
                    action = "SHIELD";
                }
                else
                {
                    action = thrust.ToString();
                }
            }

            Console.WriteLine($"{targetX} {targetY} {action}");

            // Update state
            prevX = x; prevY = y;
            prevOppX = opponentX; prevOppY = opponentY;
        }
    }

    static double Hypot(int dx, int dy)
    {
        return Math.Sqrt((double)dx * dx + (double)dy * dy);
    }

    static int Clamp(int v, int lo, int hi)
    {
        if (v < lo) return lo;
        if (v > hi) return hi;
        return v;
    }
}