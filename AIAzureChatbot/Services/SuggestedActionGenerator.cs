using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace AIAzureChatbot.Services
{
    public class SuggestedActionGenerator
    {

        //Generates a SuggestedAction object containing a List of choices (class CardAction). 
        //This is used for TextPrompts that need a list of choices (i.e. Selecting a fund)
        public static SuggestedActions GenerateSuggestedActions(List<string> toConvert) {
            var actions = new List<CardAction>();
            foreach (string action in toConvert) {
                actions.Add(new CardAction()
                {
                    Title = action,
                    Value = action,
                    Type = ActionTypes.ImBack
                });
            }

            return new SuggestedActions() { Actions = actions };
        }
    }
}
