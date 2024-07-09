using System;
using System.Collections.Generic;
using ConsoleApp1.QuestionManagers;

namespace ConsoleApp1
{
    public class Game
    {
        private readonly BaseQuestionManager _questionManager;
        private readonly UI _ui;
        private readonly string _firstName;

        public Game(BaseQuestionManager questionManager, UI ui, string firstName)
        {
            _questionManager = questionManager;
            _ui = ui;
            _firstName = firstName;
        }

        public void Start()
        {
            var questions = _questionManager.GetQuestions();
            int score = 0;

            foreach (var question in questions)
            {
                Console.WriteLine($"Hint: {question.Hint}");
                Console.Write("Your guess: ");
                string guess = Console.ReadLine().Trim().ToLower();

                bool isCorrect = guess.Equals(question.Answer.ToLower(), StringComparison.InvariantCultureIgnoreCase);
                _ui.DisplayResponse(isCorrect);

                if (isCorrect)
                {
                    score++;
                }
            }

            _ui.DisplayScore(_firstName, score, questions.Count);
        }
    }
}
