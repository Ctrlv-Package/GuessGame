using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleApp1.QuestionManagers;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                // Ask the user for their name
                Console.Write("Enter your first name: ");
                string firstName = Console.ReadLine().Trim();

                Console.WriteLine($"Hello, {firstName}! Welcome to the guessing game.");

                var questionManagers = new List<BaseQuestionManager>
                {
                    new ScienceQuestionManager(),
                    new LiteratureQuestionManager(),
                    new GeneralQuestionManager()
                };

                var allQuestions = questionManagers.SelectMany(manager => manager.GetQuestions()).ToList();

                // Ask the user to choose categories
                var categories = new HashSet<string>(allQuestions.Select(q => q.Category).Distinct());
                Console.WriteLine("Available categories:");
                foreach (var category in categories)
                {
                    Console.WriteLine($"- {category}");
                }

                Console.WriteLine("Enter the categories you want to play (comma separated). Leave empty to include all categories:");
                string selectedCategoriesInput = Console.ReadLine().Trim();

                var selectedCategories = new HashSet<string>(
                    selectedCategoriesInput.Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                );

                List<Question> selectedQuestions;

                if (selectedCategories.Count == 0)
                {
                    selectedQuestions = allQuestions;
                }
                else
                {
                    selectedQuestions = allQuestions
                        .Where(q => selectedCategories.Contains(q.Category))
                        .ToList();
                }

                if (selectedQuestions.Count < 15)
                {
                    Console.WriteLine("Not enough questions available in the selected categories. Please select more categories or add more questions.");
                    continue;
                }

                var game = new Game(new CombinedQuestionManager(selectedQuestions), new UI(), firstName);
                game.Start();

                // Ask user if they want to play again
                Console.WriteLine("Do you want to play again? (yes/no)");
                string playAgain = Console.ReadLine().Trim().ToLower();

                if (playAgain != "yes")
                {
                    break;
                }
            }
        }
    }
}
