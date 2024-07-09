using System.Collections.Generic;

namespace ConsoleApp1.QuestionManagers
{
    public abstract class BaseQuestionManager
    {
        protected List<Question> Questions { get; set; }

        protected BaseQuestionManager(List<Question> questions)
        {
            Questions = questions;
        }

        protected BaseQuestionManager() { }

        public abstract List<Question> GetQuestions();
    }
}
