using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using AIAzureChatbot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Bot.Schema;
using AIAzureChatbot.Dialogs;
using AIAzureChatbot.State;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Newtonsoft.Json;

namespace AIAzureChatbot.Bots
{
    public class MainDialogBot<T> : ActivityHandler where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotStateService _botService;
        private readonly RecognitionServices _luisService;
        private readonly ILogger _logger;

        public MainDialogBot(BotStateService botService, RecognitionServices luisService, ILogger<MainDialogBot<T>> logger, T Dialog) {
            _botService = botService;
            _dialog = Dialog;
            _luisService = luisService;
            _logger = logger;
        }

        //Triggers on every turn
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);


            //Save any changes made to the conversation or user state
            await _botService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _botService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        /*
         * triggers every time a new conversation is created
         *  i.e when the web app is refreshed, a new conversation is created and this is why you see these messages appear
         *  new members added to the conversation stored in membersAdded
         */
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                //check if the user who joined the conversation is not the Bot
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(turnContext, "Hello, I'm Jerry, the PI Investments AI Chatbot!"));
                    await turnContext.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(turnContext, "How can I help you?"));
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationState = await _botService.conversationDataAccessor.GetAsync(turnContext, () => new ConversationData());
            conversationState.SlackButtonURL = null;

            await _botService.conversationDataAccessor.SetAsync(turnContext, conversationState);
            await _botService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);

            //Start or resume MainDialog.cs
            await _dialog.Run(turnContext, _botService.dialogStateAccessor, cancellationToken);
        }
    }
}
