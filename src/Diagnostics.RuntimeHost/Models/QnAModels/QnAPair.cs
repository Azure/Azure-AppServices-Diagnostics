using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models.QnAModels
{
    public class QnAPair
    {
        public int Id;
        public string Answer;
        public List<string> Questions;
        public string Source;

        public QnAPair()
        {
            Id = 0;
            Questions = new List<string>();
        }
    }

    public class QnAPairUpdateModel
    {
        public int Id;
        public string Answer;
        public QnaPairUpdateQuestionModel Questions;
        public string Source;
    }

    public class QnaPairUpdateQuestionModel
    {
        public List<string> add;
        public List<string> delete;

        public QnaPairUpdateQuestionModel(List<string> questionsToBeAdded, List<string> questionsToBeDeleted)
        {
            add = questionsToBeAdded ?? new List<string>();
            delete = questionsToBeDeleted ?? new List<string>();
        }
    }
}
