using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIAzureChatbot.DataModels;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Newtonsoft.Json.Linq;

namespace AIAzureChatbot
{
    public class Common
    {
        public static readonly List<string> PIFundNames = new List<string> { "Mid Cap Fund", "Endeavor Fund", "Fixed Income Fund", "Core Equity Fund", "PI Fund" };
        public static int PIFundIndexInNamesList = 4; //update this if fund list changes
        public static readonly List<string> PISharesType = new List<string> { "Investor Shares", "Institutional Shares" };

        #region Fund Information Lists
        public static readonly IDictionary<string, string> investFundSymbols = new Dictionary<string, string> {
            { "PI mid cap fund", "XXXXX"},
            { "PI endeavor fund", "XXXXX"},
            { "PI core equity fund", "XXXXX"},
            { "PI fixed income fund", "XXXXX"},
            { "PI fund", "XXXXX"}
        };

        public static readonly IDictionary<string, string> instFundSymbols = new Dictionary<string, string> {
            { "PI mid cap fund", "XXXXX"},
            { "PI endeavor fund", "XXXXX"},
            { "PI core equity fund", "XXXXX"},
            { "PI fixed income fund", "XXXXX"},
            { "PI fund", "XXXXX"}
        };

        public static readonly IDictionary<string, string> tickersToFundName = new Dictionary<string, string> {
            { "XXXXX", "PI mid cap fund" },
            { "XXXXX", "PI mid cap fund" },
            { "XXXXX", "PI endeavor fund" },
            { "XXXXX", "PI endeavor fund" },
            { "XXXXX", "PI core equity fund" },
            { "XXXXX", "PI core equity fund" },
            { "XXXXX", "PI fixed income fund" },
            { "XXXXX", "PI fixed income fund" },
            { "XXXXX", "PI fund" },
            { "XXXXX", "PI fund" },
        };


        public static readonly IDictionary<string, string> tickersToIndex = new Dictionary<string, string> {
            { "XXXXX", "RMC" },
            { "XXXXX", "SPX" },
            { "XXXXX", "SPX" },
            { "XXXXX", "SPX" },
        };
        #endregion

        #region Account document lists and methods
        public static readonly string PIIraDocumentUrl = "https://www.YYYY.com/forms-and-documents/ira-forms";
        public static readonly string PINonIraDocumentUrl = "https://www.YYYY.com/forms-and-documents/account-forms";

        public static readonly List<string> IraOnlyDocumentTypes = new List<string>() { "transfer", "disclosure statement" };

        public static readonly Dictionary<string, string> IraDocumentCodeNames = new Dictionary<string, string> {
            /* [redacted] */
        };

        public static readonly Dictionary<string, string> NonIraDocumentCodeNames = new Dictionary<string, string> {
            /* [redacted] */
        };

        public static string GetAccountDocumentCodeName(bool iraAccount, AccountDocumentModel documentModel) {
            switch (iraAccount) {
                case true:
                    return IraDocumentCodeNames[documentModel.AccountDocument];
                default:
                    return NonIraDocumentCodeNames[documentModel.AccountDocument];
            }
        }
        #endregion

        #region Fund Documents Lists and methods
        public static readonly List<string> DocumentsRequireFundName = new List<string> {
            "prospectus summary",
            "fact sheet",
            "commentary"
        };

        public static readonly string FundDocumentsURL = "https://www.YYYY.com/forms-and-documents/fund-information";

        #endregion

        //usually, List-type entities have a value structured as additionalProperties: { resolution: { value: "string" } }
        // the "string" will be the normalized value of the entity that is a List type. 
        public static string GetResolutionFromEntity(EntityModel entity) {
            var resolution = (JObject)entity.AdditionalProperties["resolution"];
            var value = resolution["values"].First().Value<string>();
            return value;
        }


    }
}
