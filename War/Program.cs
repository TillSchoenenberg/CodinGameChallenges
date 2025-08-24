using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
public class Solution
{
    static void Main(string[] args)
    {
        Queue<int> player1Deck = new Queue<int>();
        Queue<int> player2Deck = new Queue<int>();

        int n = int.Parse(Console.ReadLine()); // the number of cards for player 1
        for (int i = 0; i < n; i++)
        {
            string cardp1 = Console.ReadLine(); // the n cards of player 1
            Console.Error.WriteLine(cardp1);
            player1Deck.Enqueue(ParseCard(cardp1));
        }
        int m = int.Parse(Console.ReadLine()); // the number of cards for player 2
        Console.Error.WriteLine($"PLAYER 1 Cards: {player1Deck.Count()}");
        for (int i = 0; i < m; i++)
        {
            string cardp2 = Console.ReadLine(); // the m cards of player 2
            Console.Error.WriteLine(cardp2);
            player2Deck.Enqueue(ParseCard(cardp2));
        }

        int winner = PlayGame(player1Deck, player2Deck, out int turns);
        Console.Error.WriteLine($"Winner {winner} Turn: {turns} ");
        Console.WriteLine(winner == 0 ? "PAT" : $"{winner} {turns}");
    }

    public static int PlayGame(Queue<int> player1Deck, Queue<int> player2Deck, out int turns)
    {
        turns = -1;
        while (true)
        {

            Queue<int> p1WarPile = new();
            Queue<int> p2WarPile = new();
            turns++;
            // Check for empty decks
            if (player1Deck.Count == 0 && player2Deck.Count == 0)
            {
                return 0; // PAT
            }
            if (player1Deck.Count == 0)
            {
                return 2; // Player 2 wins
            }
            if(player2Deck.Count == 0)
            {
                return 1; // Player 1 wins
            }

            int p1Card = player1Deck.Dequeue();
            int p2Card = player2Deck.Dequeue();
          
            Console.Error.WriteLine($"TURN {turns}: {p1Card} vs. {p2Card} - P1: #{player1Deck.Count} P2: #{player2Deck.Count}");

            if (p1Card > p2Card)
            {
                player1Deck.Enqueue(p1Card);
                foreach(var card in p1WarPile)
                    player1Deck.Enqueue(card);
                player1Deck.Enqueue(p2Card);
                foreach (var card in p2WarPile)
                    player1Deck.Enqueue(card);
            }
            else if (p2Card > p1Card)
            {
                player2Deck.Enqueue(p1Card);
                foreach (var card in p1WarPile)
                    player2Deck.Enqueue(card);
                player2Deck.Enqueue(p2Card);
                foreach (var card in p2WarPile)
                    player2Deck.Enqueue(card);
            }
            else
            {
                //Both are equal => War
                Console.Error.WriteLine("WAR");
                int warResult = DoWar(player1Deck, player2Deck, p1Card, p2Card, p1WarPile, p2WarPile);
                if (warResult == 0)
                    return 0; // PAT
            }
        }
    }

    private static int DoWar(Queue<int> player1Deck, Queue<int> player2Deck, int p1Card, int p2Card, Queue<int> p1WarPile, Queue<int> p2WarPile)
    {
        if (player1Deck.Count < 3 || player2Deck.Count < 3)
        {
            return 0; // PAT
        }

        p1WarPile.Enqueue(p1Card);
        p1WarPile.Enqueue(player1Deck.Dequeue());
        p1WarPile.Enqueue(player1Deck.Dequeue());
        p1WarPile.Enqueue(player1Deck.Dequeue());
        p2WarPile.Enqueue(p2Card);
        p2WarPile.Enqueue(player2Deck.Dequeue());
        p2WarPile.Enqueue(player2Deck.Dequeue());
        p2WarPile.Enqueue(player2Deck.Dequeue());

        var p1NextCard = player1Deck.Dequeue();
        var p2NextCard = player2Deck.Dequeue();

        if(p1NextCard > p2NextCard)
        {
           foreach(var card in p1WarPile)
                player1Deck.Enqueue(card);
            player1Deck.Enqueue(p1NextCard);
            foreach (var card in p2WarPile)
                player1Deck.Enqueue(card);
            player1Deck.Enqueue(p2NextCard);
        }
        else if (p2NextCard > p1NextCard)
        {
            foreach (var card in p1WarPile)
                player2Deck.Enqueue(card);
            player2Deck.Enqueue(p1NextCard);
            foreach (var card in p2WarPile)
                player2Deck.Enqueue(card);
            player2Deck.Enqueue(p2NextCard);
        }
        else
        {
            return DoWar(player1Deck, player2Deck, p1NextCard, p2NextCard, p1WarPile, p2WarPile);
        }

        return -1;
    }

    private static int ParseCard(string cardStr)
    {
        int cardValue = 0;
        switch (cardStr[0])
        {
            case '1': cardValue = 10; break;
            case 'J': cardValue = 11; break;
            case 'Q': cardValue = 12; break;
            case 'K': cardValue = 13; break;
            case 'A': cardValue = 14; break;
            default:
                cardValue = cardStr[0] - '0';
                break;
        }
        return cardValue;
    }
}