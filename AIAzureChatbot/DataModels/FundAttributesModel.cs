using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.DataModels
{
    public class FundAttributesModel
    {
        public Dictionary<string, bool> GivenAttributesDictionary = new Dictionary<string, bool> {
            /* [redacted] */
        };

        public string PIFundName { get; set; }
        public int PISharesType { get; set; } //1 - invest, 2 - inst

        public List<string> FundBasicAttributes { get; set; } 

        public List<string> FundTicker { get; set; }

        public void SetTicker() {
            FundTicker = new List<string>();
            switch (PISharesType) {
                case 0:
                    FundTicker.AddRange(new List<string> { Common.investFundSymbols[PIFundName], Common.instFundSymbols[PIFundName] });
                    break;
                case 1:
                    FundTicker.Add(Common.investFundSymbols[PIFundName]);
                    break;
                case 2:
                    FundTicker.Add(Common.instFundSymbols[PIFundName]);
                    break;
                default:
                    break;
                            
            }
            
        }
}
}
