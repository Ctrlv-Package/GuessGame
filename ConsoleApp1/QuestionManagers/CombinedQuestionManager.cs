using System.Collections.Generic;

namespace ConsoleApp1.QuestionManagers
{
    public class CombinedQuestionManager : BaseQuestionManager
    {
        public CombinedQuestionManager(List<Question> questions) : base(questions)
        {
        }

        public override List<Question> GetQuestions()
        {
            return Questions.OrderBy(q => Guid.NewGuid()).Take(15).ToList(); // Randomly select 15 questions
        }
    }
}
