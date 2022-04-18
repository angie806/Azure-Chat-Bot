using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.DataModels
{
    public class FundAttributionModel
    {
        public string FundName { get; set; }

        public string FundTicker { get; set; }

        public int ContributorsOrDetractors { get; set; } // 0 - cont, 1 - detr

        public string AttributionChar { get; set; } = null; //could be a list of attributes if more types are added

        public void SetTicker() {
            FundTicker = Common.investFundSymbols[FundName];    
        }
    }
}
