using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models.QnAModels
{
    public class UpdateKBPostBody
    {
        public AddQnAPairBody add;
        public DeleteQnaPairBody delete;
        public UpdateQnaPairBody update;
    }

    public class AddQnAPairBody
    {
        public List<QnAPair> qnaList;
        public AddQnAPairBody()
        {
            qnaList = new List<QnAPair>();
        }
    }

    public class DeleteQnaPairBody
    {
        public List<int> ids;

        public DeleteQnaPairBody()
        {
            ids = new List<int>();
        }
    }

    public class UpdateQnaPairBody
    {
        public List<QnAPairUpdateModel> qnaList;
        public UpdateQnaPairBody()
        {
            qnaList = new List<QnAPairUpdateModel>();
        }
    }
}
