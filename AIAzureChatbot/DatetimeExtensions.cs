using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot
{
    //this class is self-explanatory i hope haha
    public static class DatetimeExtensions
    {
        
        public static int GetCurrentQuarter(this DateTime date)
        {
            if (date.Month >= 1 && date.Month <= 3)
                return 1;
            else if (date.Month >= 4 && date.Month <= 6)
                return 2;
            else if (date.Month >= 7 && date.Month <= 9)
                return 3;
            else
                return 4;
        }

        public static int GetPreviousQuarter(this DateTime date) {
            var currQ = date.GetCurrentQuarter();
            if (currQ == 1) {
                return 4;
            }

            return currQ - 1;
        }
    }
}
