using System;
using System.Threading;
using System.Threading.Tasks;
using AIAzureChatbot.Services;
using AIAzureChatbot.SlackJSON;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIAzureChatbot
{
    using AIAzureChatbot.State;
    using Newtonsoft.Json;
    public class AdapterFrameworkHandler : BotFrameworkHttpAdapter
    {
        //this method is triggered right before a message an activity is sent to the user. 
        public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            foreach (Activity activity in activities) {

                //check if the activity is a message, is from the Bot, and is for a Slack user
                if (activity.Type == "message" && activity.From.Name.ToLower().Contains("bot") && activity.ChannelId == "slack") {
                    /* [redacted] */
                }
            }


            return base.SendActivitiesAsync(turnContext, activities, cancellationToken);
        }

        private ILogger _logger;
        private BotStateService _botService;

        public AdapterFrameworkHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, BotStateService botService, ConversationState conversationState = null)
            : base(configuration, logger)
        {

            _logger = logger;
            _botService = botService;

            //This method is triggered when an exception is thrown somewhere within a Dialog. 
            OnTurnError = async (turnContext, exception) =>
                {
                    // Log any leaked exception from the application.
                    logger.LogError($"Exception caught : {exception.Message} {JsonConvert.SerializeObject(exception)}");

                    // Send a catch-all apology to the user.
                    await turnContext.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(turnContext, "Sorry, it looks like something went wrong. Please refresh the page and try again."));

                    if (conversationState != null)
                    {
                        try
                        {
                            // Delete the conversationState for the current conversation to prevent the
                            // bot from getting stuck in a error-loop caused by being in a bad state.
                            await conversationState.DeleteAsync(turnContext);
                        }
                        catch (Exception e)
                        {
                            logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
                        }
                    }
                };
        }

    }
}