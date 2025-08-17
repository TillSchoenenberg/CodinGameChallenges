using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Game
{
    public Player[] Players = new Player[2];
    public Table Dishwasher;
    public Table Window;
    public Table Blueberry;
    public Table IceCream;
    public Table Strawberry;
    public Table ChoppingBoard;
    public Table Oven;
    public Table Dough;
    public List<Table> Tables = new List<Table>();
    public List<Customer> Customers = new List<Customer>();
    //public Item? CurrentOrder = null;

    public int CroissantsBaked = 0;
    public int CroissantsOrdered = 0;

    public int StrawberriesChopped = 0;
    public int ChoppedStrawberriesOrdered = 0;
    public bool OvenWasEmpty = true;
    public bool WasHoldingStrawberries = false;
}

public class Table
{
    public Position Position;
    public Item Item;
    public bool HasFunction;
    public bool IsFree => Item == null || Item.Content == "NONE";
}

public enum Ingredient
{
    ICE_CREAM,
    BLUEBERRIES,
    STRAWBERRIES,
    DISH,
    DOUGH,
    CHOPPED_STRAWBERRIES,
    CROISSANT,
}

public class Customer
{
    public int Reward;
    public Item Order;
}

public class Item
{
    public string Content;
    public Item(string content)
    {
        Content = content;
    }

    public override string ToString()
    {
        return Content;
    }

    /// <summary>
    /// compare two items for equality of their Ingredients, ignoring order.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;
        var other = (Item)obj;
        var setA = new HashSet<Ingredient>(this.Ingredients);
        var setB = new HashSet<Ingredient>(other.Ingredients);
        return setA.SetEquals(setB);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            var set = new HashSet<Ingredient>(Ingredients);
            foreach (var ing in set.OrderBy(i => i))
            {
                hash = hash * 31 + ing.GetHashCode();
            }
            return hash;
        }
    }

    public List<Ingredient> Ingredients
    {
        get
        {
            if (Content == null || Content == "NONE") return new List<Ingredient>();

            var ingredients = new List<Ingredient>();
            var strings = Content.Split('-');

            foreach (var s in strings)
            {
                Ingredient component;
                if (Enum.TryParse(s, true, out component))
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

    public bool HasItem()
    {
        return Item != null && Item.Content != "NONE";
    }

    public bool HasDish()
    {
        return Item != null && Item.Ingredients.Contains(Ingredient.DISH);
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
    public static string[,] Gameboard = new string[11, 7];


    const string CROISSANT = "CROISSANT";
    const string DOUGH = "DOUGH";
    const string DISH = "DISH";
    const string CHOPPED_STRAWBERRIES = "CHOPPED_STRAWBERRIES";
    const string STRAWBERRIES = "STRAWBERRIES";
    const string ICE_CREAM = "ICE_CREAM";
    const string BLUEBERRIES = "BLUEBERRIES";

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
                if (kitchenLine[x] == 'D') game.Dishwasher = new Table
                {
                    Position = new Position(x, i)
                    ,
                    HasFunction = true,
                    Item = new Item(DISH)
                };
                if (kitchenLine[x] == 'I') game.IceCream = new Table
                {
                    Position = new Position(x, i)
                    ,
                    HasFunction = true,
                    Item = new Item(ICE_CREAM)
                };
                if (kitchenLine[x] == 'B') game.Blueberry = new Table
                {
                    Position = new Position(x, i)
                    ,
                    HasFunction = true,
                    Item = new Item(BLUEBERRIES)
                };
                if (kitchenLine[x] == 'S') game.Strawberry = new Table
                {
                    Position = new Position(x, i)
                    ,
                    HasFunction = true,
                    Item = new Item(STRAWBERRIES)
                };
                if (kitchenLine[x] == 'C') game.ChoppingBoard = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'O') game.Oven = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'H') game.Dough = new Table
                {
                    Position = new Position(x, i)
                    ,
                    HasFunction = true,
                    Item = new Item(DOUGH)
                };
                if (kitchenLine[x] == '#') game.Tables.Add(new Table { Position = new Position(x, i) });
            }
        }

        game.Tables.Add(game.Blueberry);
        game.Tables.Add(game.IceCream);
        game.Tables.Add(game.Dishwasher);

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
        int croissantsOrdered = 0;
        int strawberriesOrdered = 0;
        for (int i = 0; i < numAllCustomers; i++)
        {
            inputs = ReadLine().Split(' ');
            string customerItem = inputs[0]; // the food the customer is waiting for
            int customerAward = int.Parse(inputs[1]); // the number of points awarded for delivering the food
            var newCustomer = new Customer { Order = new Item(customerItem), Reward = customerAward };
            customers.Add(newCustomer);
            if (newCustomer.Order.Ingredients.Contains(Ingredient.CROISSANT))
            {
                croissantsOrdered++;
            }
            if (newCustomer.Order.Ingredients.Contains(Ingredient.CHOPPED_STRAWBERRIES))
            {
                strawberriesOrdered++;
            }
        }

        // KITCHEN INPUT
        var game = ReadGame();
        game.ChoppedStrawberriesOrdered = strawberriesOrdered;
        game.CroissantsOrdered = croissantsOrdered;
        game.Customers = customers;

        while (true)
        {
            ReadTurnData(game, out Customer[] currentCustomers);

            // GAME LOGIC
            var myChef = game.Players[0];

            // 0.
            if (game.Oven.Item != null && game.Oven.Item.Content == CROISSANT)
            {
                Console.Error.WriteLine("Croissant ready in oven");
                if (myChef.HasItem())
                {
                    Console.Error.WriteLine("Clear Hands");
                    var nearestFreeTable = FindFreeTable(game, myChef.Position);
                    Use(nearestFreeTable);
                }
                else
                {
                    Console.Error.WriteLine("Get Croissant from oven");
                    Use(game.Oven.Position);
                }
                continue;
            }

            //1.#
            bool CroissantReady = game.Tables.Any(x => x.Item != null && x.Item.Content == CROISSANT) ||
                myChef.Item.Ingredients.Contains(Ingredient.CROISSANT);

            if (game.CroissantsBaked < game.CroissantsOrdered
                && game.Oven.IsFree && !CroissantReady)
            {
                Console.Error.WriteLine("Oven is empty, bake croissant.");

                if (myChef.HasItem() && myChef.Item.Content != DOUGH)
                {
                    Console.Error.WriteLine("Empty Hands.");
                    Position nearestFreeTable = FindFreeTable(game, myChef.Position);
                    Use(nearestFreeTable);
                }
                else if (myChef.HasItem() && myChef.Item.Content == DOUGH)
                {
                    Console.Error.WriteLine("Put dough in Oven");
                    Use(game.Oven.Position);
                }
                else
                {
                    Console.Error.WriteLine("Get Dough");
                    Use(game.Dough.Position);
                }
                continue;
            }

            //2.
            bool StrawberriesReady = game.Tables.Any(x => x.Item != null && x.Item.Content == CHOPPED_STRAWBERRIES)
                                        || myChef.Item.Ingredients.Contains(Ingredient.CHOPPED_STRAWBERRIES);

            if (game.StrawberriesChopped < game.ChoppedStrawberriesOrdered && !StrawberriesReady)
            {
                if (myChef.HasItem() && myChef.Item.Content != STRAWBERRIES)
                {
                    Console.Error.WriteLine("Empty Hands.");
                    Position nearestFreeTable = FindFreeTable(game, myChef.Position);
                    Use(nearestFreeTable);
                }
                else if (myChef.HasItem() && myChef.Item.Content == STRAWBERRIES)
                {
                    Console.Error.WriteLine("Chop Strawberries");
                    Use(game.ChoppingBoard.Position);
                }
                else
                {
                    Console.Error.WriteLine("Get Strawberries");
                    Use(game.Strawberry.Position);
                }
                continue;
            }

            ////3. 
            if (myChef.HasItem() && !myChef.Item.Ingredients.Contains(Ingredient.DISH))
            {
                if (myChef.Item.Ingredients.Contains(Ingredient.DOUGH)
                    || myChef.Item.Ingredients.Contains(Ingredient.STRAWBERRIES))
                {
                    Console.Error.WriteLine("Need Dish but hands full");
                    var nearestFreeTable = FindFreeTable(game, myChef.Position);
                    Use(nearestFreeTable);
                    continue;
                }

                Console.Error.WriteLine("Get Dish");
                var nearestDish = GetDish(myChef.Position, game);
                Use(nearestDish);
                continue;
            }

            //// 4.
            if (!myChef.HasItem())
            {
                Console.Error.WriteLine("Get Dish");
                var nearestDish = GetDish(myChef.Position, game);
                Use(nearestDish);
                continue;
            }

            // 5. Pick Order

            var preOrder = currentCustomers
                        .Where(x => !x.Order.Content.Contains("TART"))
                      .OrderByDescending(c => c.Reward)
                      .FirstOrDefault();

            var currentOrder = preOrder == null ?
                preOrder.Order : currentCustomers.OrderByDescending(x => x.Reward).First().Order;

            if (Debug)
                Console.Error.WriteLine($"CURRENT ORDER: {currentOrder.Content}.");

            // Invalid Order
            if (myChef.Item.Ingredients.Any(x => !currentOrder.Ingredients.Contains(x)))
            {
                Console.Error.WriteLine($"Invalid PLATE. Order was : {currentOrder.Content}");
                var nearestFreeTable = FindFreeTable(game, myChef.Position);
                Use(nearestFreeTable);
                continue;
            }


            // 6. Order completed
            if (myChef.Item.Equals(currentOrder))
            {
                Use(game.Window.Position);
                continue;
            }

            // 6.  Start Collecting Order Ingredients

            Position? prePrepared = FindPrePrepared(currentOrder, game);
            if (prePrepared != null)
            {
                Use(prePrepared);
                continue;
            }


            List<Ingredient> componentsMissing = GetMissingIngredients(currentOrder, myChef.Item);

            if (Debug)
            {
                Console.Error.WriteLine($"Missing Ingredients: {String.Join(' ', componentsMissing)}");
            }
            Position? nextComponent = GetNearestIngredient(componentsMissing, myChef, game);
            if (nextComponent == null)
            {
                Console.Error.WriteLine("ERROR: No next ingredient found.");
                Wait();
                continue;
            }

            Use(nextComponent);
        }
    }

    private static Position GetDish(Position myChefPosition, Game game)
    {
        int minDistance = int.MaxValue;
        Position? nearestPosition = null;

        if (Debug)
            Console.Error.WriteLine($"Looking for DISH");

        foreach (var table in game.Tables)
        {
            if (table.Item == null
                || table.Item.Content != DISH)
                continue;

            int distance = myChefPosition.Manhattan(table.Position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPosition = table.Position;
            }
        }

        return nearestPosition ?? game.Dishwasher.Position;
    }

    private static Position FindFreeTable(Game game, Position position)
    {
        foreach (var table in game.Tables.OrderBy(x => position.Manhattan(x.Position)))
        {
            if ((table.Item == null || table.Item.Content == "NONE") && !table.HasFunction)
            {
                Console.Error.WriteLine($"Found free table at {table.Position}");
                return table.Position;
            }
        }

        throw new InvalidOperationException("No free table found.");
    }

    private static Position? FindPrePrepared(Item order, Game game)
    {
        foreach (var table in game.Tables.OrderBy(x => game.Players[0].Position.Manhattan(x.Position)))
        {
            if (table.Item != null && table.Item.Equals(order))
            {
                Console.Error.WriteLine($"Found pre-prepared item {order.Content} at {table.Position}");
                return table.Position;
            }
        }

        return null;
    }

    private static void ReadTurnData(Game game, out Customer[] currentCustomers)
    {
        int turnsRemaining = int.Parse(ReadLine());
        string[] inputs;

        // PLAYERS INPUT
        inputs = ReadLine().Split(' ');
        game.Players[0].Update(new Position(int.Parse(inputs[0]), int.Parse(inputs[1])), new Item(inputs[2]));
        if (game.Players[0].Item.Content == CHOPPED_STRAWBERRIES && !game.WasHoldingStrawberries)
        {
            game.WasHoldingStrawberries = true;
            game.StrawberriesChopped++;
        }
        else if (game.Players[0].Item.Content != CHOPPED_STRAWBERRIES && game.WasHoldingStrawberries)
        {
            game.WasHoldingStrawberries = false;
        }

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
        if (Debug)
            Console.Error.WriteLine($"Oven Contents: {ovenContents}");
        game.Oven.Item = new Item(ovenContents);
        if (game.Oven.Item.Content == CROISSANT && game.OvenWasEmpty)
        {
            game.OvenWasEmpty = false;
            game.CroissantsBaked++;
        }
        else if (game.Oven.Item.Content != CROISSANT && !game.OvenWasEmpty)
        {
            game.OvenWasEmpty = true;
        }

        int ovenTimer = int.Parse(inputs[1]);
        int numCustomers = int.Parse(ReadLine()); // the number of customers currently waiting for food
        currentCustomers = new Customer[numCustomers];
        for (int i = 0; i < numCustomers; i++)
        {
            inputs = ReadLine().Split(' ');
            string customerItem = inputs[0];
            int customerAward = int.Parse(inputs[1]);
            currentCustomers[i] = new Customer { Order = new Item(customerItem), Reward = customerAward };
            Console.Error.WriteLine($"CurrentCustomer: {currentCustomers[i].Order}");
        }

        //if (game.CurrentOrder != null)
        //{
        //    if (currentCustomers.Select(x => x.Order.Equals(game.CurrentOrder)).Any())
        //    {
        //        if (Debug)
        //            Console.Error.WriteLine($"Current Order {game.CurrentOrder.Content} is still valid.");
        //    }
        //    else
        //    {
        //        if (Debug)
        //            Console.Error.WriteLine($"Current Order {game.CurrentOrder.Content} is no longer valid, resetting to null.");
        //        game.CurrentOrder = null;
        //    }
        //}
    }

    private static Position? GetNearestIngredient(List<Ingredient> componentsMissing, Player myChef, Game game)
    {
        if (componentsMissing.Count == 0)
        {
            return game.Window.Position;
        }

        Position myChefPosition = myChef.Position;
        var ingredients = myChef.Item.Ingredients;

        if (!myChef.Item.Ingredients.Contains(Ingredient.DISH))
        {
            return GetDish(myChefPosition, game);
        }

        int minDistance = int.MaxValue;
        Position? nearestPosition = null;

        foreach (var missing in componentsMissing)
        {
            if (Debug)
                Console.Error.WriteLine($"Looking for {missing}");

            if (missing == Ingredient.BLUEBERRIES)
            {
                int distance = myChefPosition.Manhattan(game.Blueberry.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPosition = game.Blueberry.Position;
                }
                continue;
            }

            if (missing == Ingredient.ICE_CREAM)
            {
                int distance = myChefPosition.Manhattan(game.IceCream.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPosition = game.IceCream.Position;
                }
                continue;
            }

            foreach (var table in game.Tables)
            {
                if (table.Item == null
                    || table.Item.Content != missing.ToString())
                    continue;


                int distance = myChefPosition.Manhattan(table.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPosition = table.Position;
                }
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



}