using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.DataModels
{
    public class AccountDocumentModel
    {
        public string AccountType { get; set; }
        public string AccountDocument { get; set; }

        private static List<string> IraAccountTypes = new List<string>() { "traditional ira", "roth ira", "sep ira", "ira" };

        public Nullable<int> IsIra { get; set; } = null;

        public void SetIsIra() {
            if (!string.IsNullOrEmpty(AccountType)) {
                IsIra = IraAccountTypes.Contains(AccountType) ? 1 : 0;
            }
        }
    }
}
