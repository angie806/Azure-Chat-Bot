using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.SlackJSON
{
    using Newtonsoft.Json;
    public class SlackAction
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public string Type = "button";

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

    }
}
