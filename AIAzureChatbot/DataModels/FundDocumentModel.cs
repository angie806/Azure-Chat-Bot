using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.DataModels
{
    public class FundDocumentModel
    {
        public int Year { get; set; } = 0;
        public int Month { get; set; } = 0;
        public int Quarter { get; set; } = 0;

        public string FundName { get; set; }
        public string FundDocument { get; set; }

        public Dictionary<string, int> QuarterMap = new Dictionary<string, int> {
            { "first quarter", 1 },
            { "second quarter", 2 },
            { "third quarter", 3 },
            { "fourth quarter", 4 }
        };

        public void SetQuarter(string quarter)
        {
            if (string.IsNullOrEmpty(quarter))
                return;

            if (QuarterMap.ContainsKey(quarter))
            {
                Quarter = QuarterMap[quarter];
            }
            else {
                var now = DateTime.Now;
                if (quarter == "current quarter")
                {
                    Quarter = now.GetCurrentQuarter();
                }
                else {
                    Quarter = now.GetPreviousQuarter(); // quarter == "last quarter"
                }
            }
        }

        public void SetDate(DateTime dt) {
            if (!dt.Equals(default))
            {
                this.Year = dt.Year;
                this.Month = dt.Month;
            }
        }
    }
}
