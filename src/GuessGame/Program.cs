using System;

namespace GuessGame
{
    class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();
            int target = random.Next(1, 101);
            int attempts = 0;
            Console.WriteLine("Guess the number (1-100):");
            while (true)
            {
                string? input = Console.ReadLine();
                if (!int.TryParse(input, out int guess))
                {
                    Console.WriteLine("Please enter a valid number.");
                    continue;
                }
                attempts++;
                if (guess < target)
                {
                    Console.WriteLine("Too low! Try again:");
                }
                else if (guess > target)
                {
                    Console.WriteLine("Too high! Try again:");
                }
                else
                {
                    Console.WriteLine($"Correct! You guessed it in {attempts} attempts.");
                    break;
                }
            }
        }
    }
}