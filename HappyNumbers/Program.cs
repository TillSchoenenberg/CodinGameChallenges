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
        int N = int.Parse(Console.ReadLine());
        List<string> numbers = new List<string>();
        for (int i = 0; i < N; i++)
        {
            string x = Console.ReadLine();
            numbers.Add(x);
        }

        foreach (var numberString in numbers)
        {
            string result = ProcessNumber(numberString);
            Console.WriteLine(result);
        }
    }

    public static string ProcessNumber(string number)
    {

        bool found = false;
        int counter = 0;
        string strCopy = number.ToString();
        while (counter++ < 100)
        {
            ulong sum = 0;
            char[] cs = strCopy.ToCharArray();
            foreach (var c in cs)
            {
                ulong num = (ulong)c - '0';
                num = num * num;
                sum += num;
            }

            if (sum == 1)
            {
                found = true;
                break;
            }

            strCopy = sum.ToString();
        }
        string suffix = found ? ":)" : ":(";
        return $"{number} {suffix}";
    }
}