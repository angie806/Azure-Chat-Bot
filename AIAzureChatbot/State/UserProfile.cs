using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.State
{
    public class UserProfile
    {
        public string Name { get; set; }
        public bool IsShareholder { get; set; } //false -> user is an advisor
    }
}
