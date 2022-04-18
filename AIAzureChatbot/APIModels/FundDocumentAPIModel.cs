using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.APIModels
{
    public class FundDocumentAPIModel
    {
        public string DocumentType { get; set; }
        public string FundName { get; set; }

        public string KenticoCodename { get; set; } //to be used later
        public string DocumentURL { get; set; }

        public bool HasKenticoURL { get; set; } = true;

        public int Year;
        public int Month;
        public int Quarter;
    }
}
