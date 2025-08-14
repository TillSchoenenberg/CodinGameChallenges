using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class Game
{
    public Player[] Players = new Player[2];
    public Table Dishwasher;
    public Table Window;
    public Table Blueberry;
    public Table IceCream;
    public Table Strawberry;
    public Table ChoppingBoard;
    public List<Table> Tables = new List<Table>();
    public List<Customer> Customers = new List<Customer>();
}


public enum Ingredient
{
    ICE_CREAM,
    BLUEBERRIES,
    STRAWBERRIES,
    DISH,
    CHOPPED_STRAWBERRIES,
}
public class Table
{
    public Position Position;
    public bool HasFunction;
    public Item Item;
}

public class  Customer
{
    public int Reward;
    public Item Order;
}

public class Item
{
    public string Content;
    public bool HasPlate;
    public Item(string content)
    {
        Content = content;
        HasPlate = Content.Contains(MainClass.Dish);
    }

    public List<Ingredient> Ingredients
    {
        get
        {
            if (Content == null || Content == "NONE") return new List<Ingredient>();

            var ingredients = new List<Ingredient>();
            var strings = Content.Split('-');

            foreach(var s in strings)
            {
                Ingredient component;
                if(Enum.TryParse(s, true, out component))
                {
                    ingredients.Add(component);
                }
                else
                {
                    Console.Error.WriteLine($"Unknown ingredient: {s}");
                }
            }

            return ingredients;
        }
    }
}

public class Player
{
    public Position Position;
    public Item Item;
    public Player(Position position, Item item)
    {
        Position = position;
        Item = item;
    }
    public void Update(Position position, Item item)
    {
        Position = position;
        Item = item;
    }
}

public class Position
{
    public int X, Y;
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int Manhattan(Position p2) => Math.Abs(X - p2.X) + Math.Abs(Y - p2.Y);

    public override string ToString()
    {
        return X + " " + Y;
    }
}



public class MainClass
{
    public static bool Debug = true;
    public const string Dish = "DISH";
    public static string[,] Gameboard = new string[11, 7];

    public static Game ReadGame()
    {
        var game = new Game();
        game.Players[0] = new Player(null, null);
        game.Players[1] = new Player(null, null);

        for (int i = 0; i < 7; i++)
        {
            string kitchenLine = ReadLine();
            for (var x = 0; x < kitchenLine.Length; x++)
            {
                Gameboard[x, i] = kitchenLine[x].ToString();

                if (kitchenLine[x] == 'W') game.Window = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'D') game.Dishwasher = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'I') game.IceCream = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'B') game.Blueberry = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'S') game.Strawberry = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'C') game.ChoppingBoard = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == '#') game.Tables.Add(new Table { Position = new Position(x, i) });
            }
        }

        return game;
    }

    private static void Move(Position p) => Console.WriteLine("MOVE " + p);

    private static void Use(Position p)
    {
        Console.WriteLine($"USE {p}; {Gameboard[p.X, p.Y]}");
    }

    private static void Wait()
    {
        Console.WriteLine($"WAIT");
    }

    private static string ReadLine()
    {
        var s = Console.ReadLine();
        if (Debug)
            Console.Error.WriteLine($"INPUT: {s}");
        return s;
    }


    static void Main()
    {
        string[] inputs;

        List<Customer> customers = new List<Customer>();
        // ALL CUSTOMERS INPUT: to ignore until Bronze
        int numAllCustomers = int.Parse(ReadLine());
        for (int i = 0; i < numAllCustomers; i++)
        {
            inputs = ReadLine().Split(' ');
            string customerItem = inputs[0]; // the food the customer is waiting for
            int customerAward = int.Parse(inputs[1]); // the number of points awarded for delivering the food
            customers.Add(new Customer { Order = new Item(customerItem), Reward = customerAward });
        }

        // KITCHEN INPUT
        var game = ReadGame();
        game.Customers = customers;

        while (true)
        {
            int turnsRemaining = int.Parse(ReadLine());

            // PLAYERS INPUT
            inputs = ReadLine().Split(' ');
            game.Players[0].Update(new Position(int.Parse(inputs[0]), int.Parse(inputs[1])), new Item(inputs[2]));
            inputs = ReadLine().Split(' ');
            game.Players[1].Update(new Position(int.Parse(inputs[0]), int.Parse(inputs[1])), new Item(inputs[2]));

            //Clean other tables
            foreach (var t in game.Tables)
            {
                t.Item = null;
            }
            int numTablesWithItems = int.Parse(ReadLine()); // the number of tables in the kitchen that currently hold an item
            for (int i = 0; i < numTablesWithItems; i++)
            {
                inputs = ReadLine().Split(' ');
                var table = game.Tables.First(t => t.Position.X == int.Parse(inputs[0]) && t.Position.Y == int.Parse(inputs[1]));
                table.Item = new Item(inputs[2]);
            }

            inputs = ReadLine().Split(' ');
            string ovenContents = inputs[0]; // ignore until bronze league
            int ovenTimer = int.Parse(inputs[1]);
            int numCustomers = int.Parse(ReadLine()); // the number of customers currently waiting for food
            Customer[] currentCustomers = new Customer[numCustomers];
            for (int i = 0; i < numCustomers; i++)
            {
                inputs = ReadLine().Split(' ');
                string customerItem = inputs[0];
                int customerAward = int.Parse(inputs[1]);
                currentCustomers[i] = new Customer { Order = new Item(customerItem), Reward = customerAward };
            }

            // GAME LOGIC
            // fetch a dish, pick ice cream and drop the dish on an empty table


            var myChef = game.Players[0];

            Customer? customerToServe = GetCustomerToServe(currentCustomers, myChef.Item);
            if (customerToServe == null)
            {
                Console.Error.WriteLine("No customer to serve found.");
                Use(game.Dishwasher.Position);
                continue;
            }
            List<Ingredient> componentsMissing = GetMissingIngredients(customerToServe.Order, myChef.Item);

            Position? nextComponent = GetNearestIngredient(componentsMissing, myChef, game);
            if(nextComponent == null)
            {
                Console.Error.WriteLine("ERROR: No next ingredient found.");
                Wait();
                continue;
            }

            Use(nextComponent);
        }
    }

    private static Position? GetNearestIngredient(List<Ingredient> componentsMissing, Player myChef, Game game)
    {
        if(componentsMissing.Count == 0)
        {
            return game.Window.Position;
        }

        Console.Error.WriteLine($"Components missing: {string.Join(", ", componentsMissing)}");

        Position myChefPosition = myChef.Position;
        Position nearestPosition = null;
        int minDistance = int.MaxValue;
        int distance;
        var ingredients = myChef.Item.Ingredients;
        if (componentsMissing.Contains(Ingredient.CHOPPED_STRAWBERRIES))
        {
            if (!ingredients.Contains(Ingredient.STRAWBERRIES))
                return game.Strawberry.Position;
            if(!ingredients.Contains(Ingredient.CHOPPED_STRAWBERRIES))
                return game.ChoppingBoard.Position;
        }

        if(ingredients.Contains(Ingredient.CHOPPED_STRAWBERRIES) && !ingredients.Contains(Ingredient.DISH))
        {
            return game.Dishwasher.Position;
        }

        foreach (var ingredient in componentsMissing)
        {
            switch(ingredient)
            {
                case Ingredient.ICE_CREAM:
                    distance = myChefPosition.Manhattan(game.IceCream.Position);
                    if (distance < minDistance)
                    {
                        nearestPosition = game.IceCream.Position;
                        minDistance = distance;
                    }
                    break;
                case Ingredient.BLUEBERRIES:
                    distance = myChefPosition.Manhattan(game.Blueberry.Position);
                    if (distance < minDistance)
                    {
                        nearestPosition = game.Blueberry.Position;
                        minDistance = distance;
                    }
                    break;
                case Ingredient.CHOPPED_STRAWBERRIES:
                    if(myChef.Item.Ingredients.Contains(Ingredient.STRAWBERRIES))
                    {
                        distance = myChefPosition.Manhattan(game.ChoppingBoard.Position);
                        if(distance < minDistance)
                        {
                            nearestPosition = game.ChoppingBoard.Position;
                            minDistance = distance;
                        }
                    }
                    else
                    {
                        distance = myChefPosition.Manhattan(game.Strawberry.Position);
                        if (distance < minDistance)
                        {
                            nearestPosition = game.Strawberry.Position;
                            minDistance = distance;
                        }
                    }
                    break;
                case Ingredient.DISH:
                    distance = myChefPosition.Manhattan(game.Dishwasher.Position);
                    if (distance < minDistance)
                    {
                        nearestPosition = game.Dishwasher.Position;
                        minDistance = distance;
                    }
                    break;
                default:
                    Console.Error.WriteLine($"Unknown ingredient: {ingredient}");
                    break;
            }
        }

        return nearestPosition;
    }

    private static List<Ingredient> GetMissingIngredients(Item order, Item currentDish)
    {
        var needed = order.Ingredients;
        var having = currentDish.Ingredients;

        return needed
            .Where(n => !having.Contains(n))
            .ToList();
    }



    private static Customer? GetCustomerToServe(Customer[] currentCustomers, Item item)
    {
        // not started a dish yet
        if (item == null || item.Content == "NONE")
        {
            return currentCustomers.Last(x => x.Reward == currentCustomers.Max(y => y.Reward));
        }

        //started a dish, filter matching customers
        string[] componentsReady = item.Content.Split('-');
        List<Customer> possibleCustomers = new List<Customer>();
        foreach(var customer in currentCustomers)
        {
            string contents = customer.Order.Content;
            if (!componentsReady.Any(c => contents.Contains(c)))
            {
                Console.Error.WriteLine($"No matching customer for {item.Content} in {contents}");
                continue;
            }

            possibleCustomers.Add(customer);
        }

        return possibleCustomers
            .LastOrDefault(x => x.Reward == possibleCustomers.Max(y => y.Reward));
    }
}