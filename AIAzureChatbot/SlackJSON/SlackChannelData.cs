using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.SlackJSON
{
    using AIAzureChatbot.State;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    public class SlackChannelData
    {

        public SlackChannelData(Activity activity, ConversationData convoData)
        {
            //refer to https://docs.microsoft.com/en-us/azure/bot-service/rest-api/bot-framework-rest-connector-channeldata?view=azure-bot-service-4.0#create-a-full-fidelity-slack-message
            // to see how the channel data should be structured. 

            if (activity != null)
            {


                Text = activity.Text;
                if (activity.SuggestedActions != null)
                {
                    var actions = new List<SlackAction>();
                    foreach (CardAction action in activity.SuggestedActions.Actions)
                    {
                        actions.Add(new SlackAction
                        {
                            Text = action.Title,
                            Name = action.Title,
                            Value = action.Title
                        });
                    }

                    Attachments = new SlackAttachments[] {
                        new SlackAttachments {
                            Fallback = Text,
                            Color = "#3AA3E3",
                            CallbackId = $"action_first_{actions.First().Value}_{DateTime.Now.ToShortDateString()}_{DateTime.Now.ToShortTimeString()}",
                            Actions = actions.ToArray()
                        }
                    };
                } else if (!string.IsNullOrEmpty(convoData.SlackButtonURL)) {
                    Text += $"\n> {convoData.SlackButtonURL}";
                }
            }
        }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("attachments")]
        public SlackAttachments[] Attachments { get; set; }
    }
}
