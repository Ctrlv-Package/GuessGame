using System;

namespace ConsoleApp1
{
    public class UI
    {
        private readonly Random _random = new Random();
        private readonly string[] _correctResponses = { "Well done!", "Correct answer!", "Nice job!", "You're right!", "Good work!" };
        private readonly string[] _incorrectResponses = { "Oops, that's not right.", "Try again!", "Incorrect answer.", "Not quite.", "Give it another shot." };

        public void DisplayResponse(bool isCorrect)
        {
            if (isCorrect)
            {
                Console.WriteLine(_correctResponses[_random.Next(_correctResponses.Length)]);
            }
            else
            {
                Console.WriteLine(_incorrectResponses[_random.Next(_incorrectResponses.Length)]);
            }
        }

        public void DisplayScore(string firstName, int score, int totalQuestions)
        {
            double percentage = (double)score / totalQuestions * 100;
            Console.WriteLine($"Thank you for playing, {firstName}! Your final score is: {score}/{totalQuestions} ({percentage:F2}%).");
        }

        public string GetUserInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }
    }
}
