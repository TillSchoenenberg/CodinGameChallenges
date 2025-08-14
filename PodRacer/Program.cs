using System;
using System.Collections.Generic;
using System.Globalization;

class Player
{
    // Shared race data
    static int LAPS;
    static int CP_COUNT;
    static Point[] CP;

    // Boost is shared between pods
    static bool boostAvailable = true;

    // Persistent trackers for our pods and opponents
    static PodTracker[] my = { new PodTracker(), new PodTracker() };
    static PodTracker[] opp = { new PodTracker(), new PodTracker() };

    // Decide which of our pods is the runner (kept stable with hysteresis)
    static int runnerId = -1;

    static void Main()
    {
        Console.SetOut(new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

        // Init
        LAPS = int.Parse(Console.ReadLine());
        CP_COUNT = int.Parse(Console.ReadLine());
        CP = new Point[CP_COUNT];
        for (int i = 0; i < CP_COUNT; i++)
        {
            var p = Console.ReadLine().Split(' ');
            CP[i] = new Point(int.Parse(p[0]), int.Parse(p[1]));
        }

        // Game loop
        while (true)
        {
            // Read our pods
            for (int i = 0; i < 2; i++)
            {
                var s = Console.ReadLine().Split(' ');
                my[i].Pos = new Point(int.Parse(s[0]), int.Parse(s[1]));
                my[i].V = new Vec(int.Parse(s[2]), int.Parse(s[3]));
                my[i].AngleAbs = int.Parse(s[4]);
                my[i].NextId = int.Parse(s[5]);
                my[i].UpdateProgress(CP_COUNT);
            }
            // Read opponents
            for (int i = 0; i < 2; i++)
            {
                var s = Console.ReadLine().Split(' ');
                opp[i].Pos = new Point(int.Parse(s[0]), int.Parse(s[1]));
                opp[i].V = new Vec(int.Parse(s[2]), int.Parse(s[3]));
                opp[i].AngleAbs = int.Parse(s[4]);
                opp[i].NextId = int.Parse(s[5]);
                opp[i].UpdateProgress(CP_COUNT);
            }

            // Decide/lock roles
            DecideRunner();

            // Build orders for both pods, preserving input order
            var orders = new string[2];
            for (int i = 0; i < 2; i++)
            {
                if (i == runnerId) orders[i] = ControlRunner(my[i], my[1 - i]);
                else orders[i] = ControlCamper(my[i], my[1 - i]);
            }

            // Output
            Console.WriteLine(orders[0]);
            Console.WriteLine(orders[1]);
        }
    }

    // ================== Runner control ==================

    static string ControlRunner(PodTracker me, PodTracker mate)
    {
        var cp = CP[me.NextId];
        var next = CP[(me.NextId + 1) % CP_COUNT];

        // Racing target: aim beyond checkpoint toward the next one, with velocity compensation
        Vec toNext = (next - cp).Norm();
        int turnOut = Clamp((int)(me.Speed() * 0.6) + 350, 300, 1400);
        int leadFactor = 1;
        Point aim = (cp + toNext * turnOut) - (me.V * leadFactor);

        // If already within the checkpoint circle, push through the exit line
        double distToCp = (me.Pos - cp).Len();
        if (distToCp < 650)
        {
            int exitPush = 800;
            aim = cp + toNext * exitPush;
        }

        // Thrust control based on relative angle
        double angleToAim = RelativeAngle(me, aim);
        int absA = Abs(angleToAim);
        int thrust = 100;
        if (absA > 100) thrust = 0;
        else if (absA > 70) thrust = 25;

        // Distance-aware braking (scale with speed)
        int brakeDist = (int)Math.Max(1200, me.Speed() * 5.5);
        if (distToCp < brakeDist)
        {
            if (absA > 45) thrust = Math.Min(thrust, 30);
            else thrust = Math.Min(thrust, 60);
        }

        // BOOST once on a straight, long segment
        string action = thrust.ToString(CultureInfo.InvariantCulture);
        if (boostAvailable && absA < 10 && distToCp > 5500 && me.Speed() > 250)
        {
            double turnAfter = AngleBetween(cp, next, CP[(me.NextId + 2) % CP_COUNT]);
            if (Abs(turnAfter) < 25)
            {
                action = "BOOST";
                boostAvailable = false;
            }
        }

        // Shield if an opponent is about to ram
        if (ShouldShield(me, NearestOpponent(me)))
            action = "SHIELD";

        return $"{aim.X} {aim.Y} {action}";
    }

    // ================== Camper control (checkpoint blocker) ==================

    static string ControlCamper(PodTracker me, PodTracker mate)
    {
        // Pick the leading opponent and camp their next checkpoint
        int t = (opp[0].TotalPassed > opp[1].TotalPassed) ? 0
              : (opp[0].TotalPassed < opp[1].TotalPassed) ? 1
              : (opp[0].DistToNext(CP) <= opp[1].DistToNext(CP) ? 0 : 1);

        var targetOpp = opp[t];
        Point cp = CP[targetOpp.NextId];

        // Inbound direction: where the opponent is coming from
        Vec inbound = (cp - targetOpp.Pos).Norm();
        if (inbound.Len() < 1e-6) inbound = (cp - me.Pos).Norm(); // fallback

        // Guard geometry: just outside the cp radius, plus a slight lateral offset to cover the entry arc
        int guardRad = 780; // place 780 units from center, outside 600 radius
        int lateral = 180;  // side-step to force a collision or detour
        Vec perp = new Vec(-inbound.Y, inbound.X).Norm();
        // Choose lateral side based on which keeps us between opp and cp
        double side = Math.Sign(((me.Pos - cp).X * perp.X + (me.Pos - cp).Y * perp.Y));
        if (side == 0) side = 1;
        Point guardPoint = cp - inbound * guardRad + perp * (lateral * side);

        // If the opponent changed checkpoint or we're far off, beeline to guardPoint
        double dGuard = (me.Pos - guardPoint).Len();

        // Thrust: enough to hold position once arrived, stronger when repositioning
        double angleToGuard = RelativeAngle(me, guardPoint);
        int absA = Abs(angleToGuard);
        int thrust;
        if (dGuard > 2500) thrust = absA > 90 ? 50 : 100;
        else if (dGuard > 900) thrust = absA > 90 ? 40 : 85;
        else if (dGuard > 400) thrust = absA > 90 ? 30 : 65;
        else thrust = 35; // hover near spot to keep blocking arc

        string action = thrust.ToString(CultureInfo.InvariantCulture);

        // If an immediate collision is likely, shield to anchor the block
        if (ShouldShield(me, targetOpp))
            action = "SHIELD";

        // Avoid friendly hits: if our runner is coming through, ease off
        var runner = my[runnerId];
        if ((runner.Pos - me.Pos).Len() < 1100 && runner.TotalPassed >= me.TotalPassed)
            action = Math.Min(thrust, 40).ToString(CultureInfo.InvariantCulture);

        return $"{guardPoint.X} {guardPoint.Y} {action}";
    }

    // ================== Utilities & heuristics ==================

    static void DecideRunner()
    {
        // Stable role with hysteresis: prefer the one ahead; switch only on clear advantage
        int suggested = CompareProgress(my[0], my[1]) >= 0 ? 0 : 1;
        if (runnerId == -1) { runnerId = suggested; return; }

        int current = runnerId, other = 1 - runnerId;
        int diff = my[other].TotalPassed - my[current].TotalPassed;
        if (diff >= 2 || (diff == 1 && my[other].DistToNext(CP) + 600 < my[current].DistToNext(CP)))
            runnerId = other;
    }

    static int CompareProgress(PodTracker A, PodTracker B)
    {
        if (A.TotalPassed != B.TotalPassed) return A.TotalPassed > B.TotalPassed ? 1 : -1;
        double dA = A.DistToNext(CP), dB = B.DistToNext(CP);
        if (Math.Abs(dA - dB) > 1e-6) return dA < dB ? 1 : -1;
        return 0;
    }

    static PodTracker NearestOpponent(PodTracker me)
    {
        var o0 = (opp[0].Pos - me.Pos).Len();
        var o1 = (opp[1].Pos - me.Pos).Len();
        return o0 <= o1 ? opp[0] : opp[1];
    }

    static bool ShouldShield(PodTracker me, PodTracker threat)
    {
        Vec relV = me.V - threat.V;
        Vec relP = threat.Pos - me.Pos;
        double dist = relP.Len();
        double closing = relV.X * relP.X + relV.Y * relP.Y; // dot < 0 means approaching
        return dist < 800 && relV.Len() > 400 && closing < 0;
    }

    static double RelativeAngle(PodTracker me, Point target)
    {
        Vec to = (target - me.Pos).Norm();
        double angleToTarget = Math.Atan2(to.Y, to.X) * 180.0 / Math.PI; // 0 East, +90 South
        double rel = NormalizeAngle(angleToTarget - me.AngleAbs);
        return rel;
    }

    static double AngleBetween(Point a, Point b, Point c)
    {
        Vec v1 = (a - b).Norm();
        Vec v2 = (c - b).Norm();
        double ang1 = Math.Atan2(v1.Y, v1.X) * 180.0 / Math.PI;
        double ang2 = Math.Atan2(v2.Y, v2.X) * 180.0 / Math.PI;
        return NormalizeAngle(ang2 - ang1);
    }

    static double NormalizeAngle(double deg)
    {
        while (deg <= -180) deg += 360;
        while (deg > 180) deg -= 360;
        return deg;
    }

    static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    static int Abs(double d) => (int)Math.Abs(d);

    // ================== Types ==================

    struct Point
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
        public static Vec operator -(Point a, Point b) => new Vec(a.X - b.X, a.Y - b.Y);
        public static Point operator +(Point a, Vec v) => new Point(a.X + (int)v.X, a.Y + (int)v.Y);
        public static Point operator -(Point a, Vec v) => new Point(a.X - (int)v.X, a.Y - (int)v.Y);
        public override string ToString() => $"{X} {Y}";
        public double Len() => Math.Sqrt((double)X * X + (double)Y * Y);
    }

    struct Vec
    {
        public double X, Y;
        public Vec(double x, double y) { X = x; Y = y; }
        public static Vec operator +(Vec a, Vec b) => new Vec(a.X + b.X, a.Y + b.Y);
        public static Vec operator -(Vec a, Vec b) => new Vec(a.X - b.X, a.Y - b.Y);
        public static Vec operator *(Vec a, int k) => new Vec(a.X * k, 0);
        public static Vec operator *(Vec a, double k) => new Vec(a.X * k, a.Y * k);
        public double Len() => Math.Sqrt(X * X + Y * Y);
        public Vec Norm()
        {
            double l = Len();
            if (l < 1e-9) return new Vec(0, 0);
            return new Vec(X / l, Y / l);
        }
    }

    class PodTracker
    {
        public Point Pos;
        public Vec V;
        public int AngleAbs;
        public int NextId;
        public int PrevNextId = -1;

        // Monotonic progress as number of checkpoints passed
        public int TotalPassed = 0;

        public void UpdateProgress(int N)
        {
            if (PrevNextId == -1) { PrevNextId = NextId; return; }
            if (NextId != PrevNextId)
            {
                int expected = (PrevNextId + 1) % N;
                if (NextId == expected) TotalPassed++;
                else
                {
                    int delta = (NextId - PrevNextId + N) % N;
                    if (delta > 0) TotalPassed += delta;
                }
                PrevNextId = NextId;
            }
        }

        public int Speed() => (int)new Vec(V.X, V.Y).Len();
        public double DistToNext(Point[] cps) => (Pos - cps[NextId]).Len();
    }
}