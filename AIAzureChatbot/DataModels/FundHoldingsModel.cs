using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.DataModels
{
    public class FundHoldingsModel
    {
        public string Company { get; set; }

        public List<string> FundName { get; set; }

        public bool AllFunds { get; set; } = false;

        public List<string> HoldingAttributes { get; set; }
    }
}
