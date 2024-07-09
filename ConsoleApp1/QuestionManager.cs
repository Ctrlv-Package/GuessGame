using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1
{
    public class QuestionManager
    {
        private readonly List<Question> _questions;

        public QuestionManager(List<Question> questions)
        {
            _questions = questions;
        }

        public List<Question> GetRandomQuestions(int count)
        {
            var random = new Random();
            return _questions.OrderBy(q => random.Next()).Take(count).ToList();
        }
    }
}
