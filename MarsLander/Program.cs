using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;



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

    public Vector2 ThrustVector
    {
        get
        {
            Math.Cos(RotationAngle);
            Math.

            return new Vector2(Thrust, RotationAngle);
        }
    }

    public Vector2 CurrentVector
    {
        get
        {
            return new Vector2(H_Speed, V_Speed) ;
        }
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
class Game
{
    const int WIDTH = 7000;
    const int HEIGHT = 3000;
    const int MIN_WIDTH_LANDING_SITE = 1000;
    const double MAX_VERTICAL_SPEED = 40;
    const double MAX_HORIZONTAL_SPEED = 20;
    const double MIN_ANGLE = -90;
    const double MAX_ANGLE = 90;
    static void Main(string[] args)
    {
        LandingArea area = ReadLandingArea();

        // game loop
        while (true)
        {
            Lander lander = ReadLander();

            

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");


            // R P. R is the desired rotation angle. P is the desired thrust power.
            Console.WriteLine("-20 3");
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

    private static LandingArea ReadLandingArea()
    {
        List<(int x, int y)> areaList = new();
        string[] inputs = null;
        int N = int.Parse(Console.ReadLine()); // the number of points used to draw the surface of Mars.
        for (int i = 0; i < N; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int landX = int.Parse(inputs[0]); // X coordinate of a surface point. (0 to 6999)
            int landY = int.Parse(inputs[1]); // Y coordinate of a surface point. By linking all the points together in a sequential fashion, you form the surface of Mars.
            areaList.Add((landX, landY));
        }

        LandingArea area = FindLandingArea(areaList);
        return area;
    }

    private static LandingArea? FindLandingArea(List<(int x, int y)> areaList)
    {
        var sortedList = areaList.OrderBy(x => x.x);

        int previousY = -1;
        int previousX = 0;
        foreach (var area in sortedList)
        {
            if(area.x - previousX >= MIN_WIDTH_LANDING_SITE 
                && (previousY == -1 || area.y == previousY))
                return new LandingArea() { Height = area.y, StartX = previousX, EndX = area.x };

            previousY = area.y;
            previousX = area.x;
        }

        throw new InvalidDataException("No landing area found!");
    }
}