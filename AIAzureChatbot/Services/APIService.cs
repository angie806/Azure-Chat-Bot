using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AIAzureChatbot.APIModels;
using AIAzureChatbot.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;

namespace AIAzureChatbot.Services
{
    public class APIService
    {
        /* [redacted] */
    }


    /*
    * Attribute Node dictionaries are used as such:
    *      Take the LUIS normalized entity value and get the Attribute Node
    *          The key -> used for getting the JSON value from the API data 
    *          Formatted name -> used for printing out a readable value for the user
    *          Prepend/Postpend string -> used for values that are %s or $s
    *          IsDouble -> used to parse a number and add commas or decimal points if needed
    *          IsAsOfDate -> used to append an "as of date" to the value
    *          
    */
    public class AttributeNode {
        public string key { get; set; }
        public string formattedName { get; set; }
        public bool IsAsOfDate { get; set; } = false;

        public string prependString { get; set; }
        public string postpendString { get; set; }

        public bool IsDouble { get; set; }

        public AttributeNode(string k, string fn, string prepend = "", string postpend = "", bool aod = false, bool iD = false) {
            key = k;
            formattedName = fn;
            IsAsOfDate = aod;
            prependString = prepend;
            postpendString = postpend;
            IsDouble = iD;
        }
    }
}
