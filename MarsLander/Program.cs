using System.Numerics;



internal class Gene
{
    public int Thrust { get; set; }
    public int Angle { get; set; }

    internal Gene()
    {
        Thrust = Random.Shared.Next(-1, 1);
        Angle = Random.Shared.Next(-15,15);    
    }

    public void Mutate()
    {
        int rand = Random.Shared.Next(0, 100);
        if (rand >= 20 && rand < 30)
        {
            MutateThrust();
        }
        else if (rand >= 10)
        {
            MutateAngle();
        }
        else if (rand >= 0)
        {
            MutateThrust();
            MutateAngle();
        }
    }

    public Gene Clone()
    {
        return new Gene() { Thrust = Thrust, Angle = Angle };
    }


    private void MutateThrust()
    {

        //Mutate Thrust
        int minThrust = this.Thrust - 1;
        int maxThrust = this.Thrust + 1;
        if (maxThrust > 4)
            maxThrust = 4;
        if (minThrust < 0)
            minThrust = 0;

        int newThrust = Random.Shared.Next(minThrust, maxThrust);
        this.Thrust = newThrust;
    }
    private void MutateAngle()
    {

        //Mutate Angle
        int minAngle = this.Angle - 15;
        int maxAngle = this.Angle + 15;
        if (minAngle < -90)
            minAngle = -90;
        if (maxAngle > 90)
            maxAngle = 90;

        int newAngle = Random.Shared.Next(minAngle, maxAngle);
        this.Angle = newAngle;
    }
}

internal class SimulationResult
{
    internal readonly int Score;
    internal readonly Chromosome Chromosome;

    internal SimulationResult(Chromosome chromosome, int score)
    {
        Chromosome = chromosome;
        Score = score;
    }
}

internal class Chromosome
{
    internal const int MAX_ELEMENTS = 10;
    public List<Gene> GeneList { get; private set; } = new List<Gene>();

    public bool TryAddGene(Gene gene)
    {
        if (GeneList.Count >= MAX_ELEMENTS)
            return false;

        GeneList.Add(gene);
        return true;
    }

    public void Mutate()
    {
        foreach (Gene gene in GeneList)
            gene.Mutate();
    }

    public Chromosome Clone()
    {
        Chromosome clone = new Chromosome();    
        foreach(var gene in GeneList)
        {
            clone.GeneList.Add(gene.Clone());
        }

        return clone;
    }
}

internal class Simulator(LandingArea _landingArea, Lander _currentLander)
{
    const int POPULATION_SIZE = 100;
    static int Generation = 0;

    internal List<Chromosome> GenerateInitialPopulation()
    {
        Generation = 1;
        List<Chromosome> initialPopulation = new List<Chromosome>();
        for (int i = 0; i < POPULATION_SIZE; i++)
        {
            Chromosome chromosome = new Chromosome();
            for(int j = 0; j < Chromosome.MAX_ELEMENTS; j++)
            {
                chromosome.GeneList.Add(new Gene());
            }

            initialPopulation.Add(chromosome);
        }

        return initialPopulation; 
    }

    internal List<Chromosome> GenerateNextGeneration(List<SimulationResult> previousPopulation)
    {
        List<Chromosome> nextPopulation = new List<Chromosome>();
        int scoreSum = previousPopulation.Sum(x => x.Score);
        var orderedPrevious = previousPopulation.OrderBy(x => x.Score);

        while(nextPopulation.Count < POPULATION_SIZE)
        {
            Chromosome parent1 = RouletteSelection(orderedPrevious, scoreSum);
            Chromosome parent2 = RouletteSelection(orderedPrevious, scoreSum);

            Chromosome offspring = Breed(parent1, parent2);

            if(Random.Shared.Next(0, 100) < 5)
                offspring.Mutate();

            nextPopulation.Add(offspring);
        }

        Generation++;
        return nextPopulation;
    }

    private Chromosome Breed(Chromosome parent1, Chromosome parent2)
    {
        Chromosome offspring = new Chromosome();

        if (parent1.GeneList.Count != parent2.GeneList.Count)
            throw new InvalidDataException($"One Chromosome shorter than the other");

        int maxElements = Math.Max(parent1.GeneList.Count, parent2.GeneList.Count);

        for (int i = 0; i < maxElements; i++)
        {
            Gene gene1 = parent1.GeneList[i];
            Gene gene2 = parent2.GeneList[i];

            Gene crossedGene = Random.Shared.Next(0, 2) == 0 ? gene1 : gene2;
            offspring.TryAddGene(crossedGene);
        }

        return offspring;
    }

    private Chromosome RouletteSelection(IOrderedEnumerable<SimulationResult> orderedPrevious, int scoreSum)
    {
        int sum = 0;
        int rand = Random.Shared.Next(0, scoreSum);

        foreach(var item in orderedPrevious)
        {
            sum += item.Score;
            if (sum > rand)
                return item.Chromosome;
        }

        return orderedPrevious.Last().Chromosome;
    }

    /// <summary>
    /// Calculates the last position of lander, scores that Position
    /// </summary>
    /// <todo>
    /// run in a seperate thread, so that the calculation process can run while former list gets executed?
    /// </todo>
    /// <param name="chromosome"></param>
    /// <returns></returns>
    internal SimulationResult ScoreFittness(Chromosome chromosome)
    {
        int score = 0;
        var lander = CurrentLander;
        foreach(var gene in chromosome.GeneList)
        {
            lander = lander.ApplyTurn(gene.Angle, gene.Thrust);
            if (lander.IsLanded(LandingArea))
                return new SimulationResult(chromosome, int.MaxValue);
            if (lander.IsCrashed())
                return new SimulationResult(chromosome, int.MinValue);
        }

        int distX = Game.WIDTH - Math.Abs(lander.X - LandingArea.Middle);
        int fuelScore = lander.Fuel;
        score += distX * 100 + fuelScore * 10;
        score -= Math.Abs(lander.Y - LandingArea.Height);
        score -= Math.Abs(lander.H_Speed);
        score -= Math.Abs(lander.V_Speed);
        score -= Math.Abs(lander.RotationAngle);

        if(score < 0)
            score = 0;

        return new SimulationResult(chromosome, score);
    }

    internal LandingArea LandingArea { get; } = _landingArea;
    internal Lander CurrentLander { get; } = _currentLander;
}
internal class Lander
{
    const float GRAVITY = 3.711f; // m / s²
    public int X;
    public int Y;
    public int H_Speed;
    public int V_Speed;
    public int Fuel;
    public int Thrust;
    public int RotationAngle;

    public Vector2 GravityVector
    {
        get
        {
            return new Vector2(0, -GRAVITY);
        }
    }


    private Vector2 GetThrustVector(int thrust, int angleDegrees)
    {
        int distance = thrust;
        double angleRadians = angleDegrees * Math.PI / 180.0;

        double x = Math.Cos(angleRadians) * distance;
        double y = Math.Sin(angleRadians) * distance;

        // Resulting vector
        Vector2 result = new Vector2((float)x, (float)y);

        return result;
    }

    public Vector2 CurrentSpeedVector
    {
        get
        {
            return new Vector2(H_Speed, V_Speed);
        }
    }

    public Lander ApplyTurn(int angleDelta, int thrustDelta)
    {
        if (angleDelta > 15 || angleDelta < -15)
        {
            throw new InvalidOperationException($"{angleDelta} not valid max delta for angle is -15° - +15 °");
        }

        int newAngle = angleDelta + RotationAngle;
        if (newAngle > 90 || newAngle < -90)
        {
            Console.Error.WriteLine($"{newAngle}° resulting anlge is invaliv. (-90;90)");
        }
        newAngle = Math.Clamp(newAngle, -90, 90);
        if (thrustDelta < -1 || thrustDelta > 1)
        {
            throw new InvalidOperationException($"{angleDelta} not valid max delta for angle is -15° - +15 °");
        }
        int newThrust = thrustDelta + Thrust;
        if (newThrust > 4 || newThrust < 0)
        {
            Console.Error.WriteLine($"{newThrust} Thrust invalid. (0;4)");
        }
        newThrust = Math.Clamp(newThrust, 0, 4);

        Vector2 resultingVector = CurrentSpeedVector + GetThrustVector(newAngle, newThrust) + GravityVector;

        Lander newLander = new Lander()
        {
            Fuel = this.Fuel - newThrust,
            RotationAngle = newAngle,
            Thrust = newThrust,
            X = this.X + (int)resultingVector.X,
            Y = this.Y + (int)resultingVector.Y,
            H_Speed = (int)resultingVector.X,
            V_Speed = (int)resultingVector.Y,
        };

        return newLander;
    }

    private Lander Clone()
    {
        return new Lander()
        {
            Fuel = this.Fuel,
            RotationAngle = this.RotationAngle,
            Thrust = this.Thrust,
            X = this.X,
            Y = this.Y,
            H_Speed = this.H_Speed,
            V_Speed = this.V_Speed,
        };
    }

    internal bool IsLanded(LandingArea landingArea)
    {   
        if (V_Speed > Game.MAX_VERTICAL_SPEED || H_Speed > Game.MAX_HORIZONTAL_SPEED)
            return false;
        

        if (RotationAngle != 0)
            return false;

        if (X < landingArea.StartX && X > landingArea.EndX)
            return false;

        if (Y > landingArea.Height - Game.MAX_VERTICAL_SPEED)
            return false;

        if (Y - V_Speed <= landingArea.Height)
            return true;

        return false;
    }

    /// <summary>
    /// This function checks if the lander is crashed, however IsLanded must be called first.
    /// 
    /// </summary>
    /// <todo>make the IsLanded and IsCrashed independent of each other, maybe a GetLanderStatus function?</todo>
    /// <returns></returns>
    internal bool IsCrashed()
    {
        int TerrainHeight = GetTerrainHeight(Game.AreaList);

        if(TerrainHeight <= Y)
            return true;

        return false;
    }

    private int GetTerrainHeight(List<(int x, int y)> areaList)
    {
        throw new NotImplementedException();
    }
}

internal class LandingArea
{
    public int Height;
    public int StartX;
    public int EndX;

    public int Middle
    {
        get
        {
            return StartX + ((EndX - StartX) / 2);
        }
    }
}

/**
 * Save the Planet.
 * Use less Fossil Fuel.
 **/
internal class Game
{
    internal const int WIDTH = 7000;
    internal const int HEIGHT = 3000;
    internal const int MIN_WIDTH_LANDING_SITE = 1000;
    internal const double MAX_VERTICAL_SPEED = 40;
    internal const double MAX_HORIZONTAL_SPEED = 20;
    internal const double MIN_ANGLE = -90;
    internal const double MAX_ANGLE = 90;
    static void Main(string[] args)
    {
        LandingArea area = ReadLandingArea();

        // game loop
        while (true)
        {
            Lander lander = ReadLander();

            Simulator simulator = new Simulator(area, lander);
            var initialPopulation = simulator.GenerateInitialPopulation();
            
            //if Any(score == int.MaxValue) => choose this and stop optimizing

            var nextPopulation = simulator.GenerateNextGeneration(initialPopulation);

        }

    }

    private static Lander ReadLander()
    {
        var inputs = Console.ReadLine().Split(' ');
        int X = int.Parse(inputs[0]);
        int Y = int.Parse(inputs[1]);
        int HS = int.Parse(inputs[2]); // the horizontal speed (in m/s), can be negative.
        int VS = int.Parse(inputs[3]); // the vertical speed (in m/s), can be negative.
        int F = int.Parse(inputs[4]); // the quantity of remaining fuel in liters.
        int R = int.Parse(inputs[5]); // the rotation angle in degrees (-90 to 90).
        int P = int.Parse(inputs[6]); // the thrust power (0 to 4).

        Lander lander = new Lander()
        {
            X = X,
            Y = Y,
            Fuel = F,
            H_Speed = HS,
            V_Speed = VS,
            RotationAngle = R,
            Thrust = P,
        };

        return lander;
    }

    public static List<(int x, int y)> AreaList { get; private set; } = new();

    private static LandingArea ReadLandingArea()
    {
        string[] inputs = null;
        int N = int.Parse(Console.ReadLine()); // the number of points used to draw the surface of Mars.
        for (int i = 0; i < N; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int landX = int.Parse(inputs[0]); // X coordinate of a surface point. (0 to 6999)
            int landY = int.Parse(inputs[1]); // Y coordinate of a surface point. By linking all the points together in a sequential fashion, you form the surface of Mars.
            AreaList.Add((landX, landY));
        }

        LandingArea area = FindLandingArea(AreaList);
        return area;
    }

    private static LandingArea? FindLandingArea(List<(int x, int y)> areaList)
    {
        var sortedList = areaList.OrderBy(x => x.x);

        int previousY = -1;
        int previousX = 0;
        foreach (var area in sortedList)
        {
            if (area.x - previousX >= MIN_WIDTH_LANDING_SITE
                && (previousY == -1 || area.y == previousY))
                return new LandingArea() { Height = area.y, StartX = previousX, EndX = area.x };

            previousY = area.y;
            previousX = area.x;
        }

        throw new InvalidDataException("No landing area found!");
    }
}