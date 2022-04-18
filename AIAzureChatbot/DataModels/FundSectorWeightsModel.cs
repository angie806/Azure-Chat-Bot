using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.DataModels
{
    public class FundSectorWeightsModel
    {
        public string Sector { get; set; }

        public List<string> Funds { get; set; }

        public void AddFund(string fundName) {
            if (fundName == "any fund" || fundName == "all funds")
            {
                Funds.AddRange(Common.investFundSymbols.Values);
            }
            else {
                Funds.Add(Common.investFundSymbols[fundName]);
            }
        }
    }
}
