using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AIAzureChatbot.Dialogs.FundDialogs;
using AIAzureChatbot.Services;
using AIAzureChatbot.State;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace AIAzureChatbot.Dialogs
{
    public class MainDialog : CancelAndHelpDialog
    {
        private readonly BotStateService _botService;
        private readonly RecognitionServices _luisService;

        private Dictionary<string, string> IntentToDialogId;

        public MainDialog(BotStateService botService, RecognitionServices luisService) : base(nameof(MainDialog), botService, luisService)
        {
            _botService = botService;
            _luisService = luisService;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            var waterfallSteps = new WaterfallStep[] {
                InitialStepAsync,
                SendInitialPromptAsync,
                DispatchRecognitionStepAsync,
                FinalStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.waterfall", waterfallSteps));
            AddDialog(new AccountDocumentsDialog($"{nameof(MainDialog)}.accountDocuments", _botService, _luisService));
            AddDialog(new FundDocumentsDialog($"{nameof(MainDialog)}.fundDocuments", _botService, _luisService));
            AddDialog(new FundHoldingsDialog($"{nameof(MainDialog)}.fundHoldings", _botService, _luisService));
            AddDialog(new FundSpecificAttributesDialog($"{nameof(MainDialog)}.specificFundAttr", _botService, _luisService));
            AddDialog(new FundAttributionDialog($"{nameof(MainDialog)}.fundAttribution", _botService, _luisService));
            AddDialog(new FundSectorWeightsDialog($"{nameof(MainDialog)}.fundSectorWeights", _botService, _luisService));

            AddDialog(new TextPrompt($"{nameof(MainDialog)}.promptIntro"));

            IntentToDialogId = new Dictionary<string, string> {
                /* [redacted] */
            };

            InitialDialogId = $"{nameof(MainDialog)}.waterfall";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> SendInitialPromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> DispatchRecognitionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {

            UserProfile profile = await _botService.profileStateAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            ConversationData conversationState = await _botService.conversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData());
            conversationState.IsCancellable = true;

            //get the intent from LUIS Dispatch. Identifies whether message should be passed onto the main LUIS app or QnA Maker. 
            var recognizerResult = await _luisService.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            (string intent, double score) = recognizerResult.GetTopScoringIntent();
            var luisResult = recognizerResult.Properties["luisResult"] as LuisResult;

            
            switch (intent)
            {
                case /* [redacted] */:
                    return await ProcessQnaIntentAsync(stepContext, cancellationToken);
                case /* [redacted] */:
                    return await ProcessLuisIntentAsync(stepContext, cancellationToken, luisResult);
                default:
                    await ProcessRecognitionError(ChannelFormattingService.FormatMessages(stepContext.Context, "Sorry, I do not know what you mean.",
                        new List<Func<string, string>> { ToHtml.MessageWrapInSpan }), stepContext, cancellationToken);
                    return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ProcessQnaIntentAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {

            //send message to QnA Maker
            var results = await _luisService.QnaRecognizer.GetAnswersAsync(stepContext.Context);

            //if an answer was found, respond with the answer. 
            //otherwise, respond with error. 
            if (results.Any())
            {
                ConversationData conversationData = await _botService.conversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData());
                conversationData.FailedAttemptsCount = 0;

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatMessages(stepContext.Context, "If this does not answer your question, please try structuring your question in a " +
                    "different way and/or to be more specific", new List<Func<string, string>> { ToHtml.MessageWrapInSpan }), cancellationToken: cancellationToken);
            }
            else
            {
                await ProcessRecognitionError(ChannelFormattingService.FormatMessages(stepContext.Context, "Sorry, I was not able to find an answer to your question. Please try structuring your question in a " +
                    "different way and/or to be more specific", new List<Func<string, string>> { ToHtml.MessageWrapInSpan }), stepContext, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessLuisIntentAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken, LuisResult luisResult) {
            UserProfile profile = await _botService.profileStateAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            ConversationData conversationData = await _botService.conversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData());

            //get the connected service result from the LUIS Main App. This will contain all the specific entities and intents. 
            var intent = luisResult.ConnectedServiceResult.TopScoringIntent.Intent.Trim();

            //if user is sending a greeting, greet back
            if (intent == /* [redacted] */) {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatMessages(stepContext.Context, $"Hello, how can I help you today?",
                        new List<Func<string, string>> { ToHtml.MessageWrapInSpan }));

                conversationData.FailedAttemptsCount = 0;
                return await stepContext.NextAsync(null, cancellationToken);
            //if the user has a specific intent, find the dialog ID associated with the intent in the Dictionary and begin
            } else if (IntentToDialogId.ContainsKey(intent)) {

                conversationData.FailedAttemptsCount = 0;
                return await stepContext.BeginDialogAsync(IntentToDialogId[intent], luisResult, cancellationToken);
            //otherwise...
            } else {

                await ProcessRecognitionError(ChannelFormattingService.FormatMessages(stepContext.Context, "Sorry, I do not know what you mean.",
                         new List<Func<string, string>> { ToHtml.MessageWrapInSpan }), stepContext, cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);

            }
        }

        private async Task ProcessRecognitionError(string message, WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            ConversationData conversationData = await _botService.conversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData());

            //incremene the failed counter
            conversationData.FailedAttemptsCount += 1;
            //if the user has failed more than 3 times
            if (conversationData.FailedAttemptsCount >= 3)
            {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatMessages(stepContext.Context, "Sorry, I am still unable to understand your question. Please call Shareholder Services at " +
                    "(800) 999-3505 for further asssistance.", new List<Func<string, string>> { ToHtml.MessageWrapInSpan }), null, null, cancellationToken);
                
            }
            //otherwise respond with "sorry, I do not know what you mean." (this message may be variated so it is passed as a paramter)
            else {
                await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var conversationData = await _botService.conversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData());
            conversationData.IsCancellable = true;

            //this condition is satisfied when the user sends "cancel" or "help"
            if (conversationData.DidCancel) {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatMessages(stepContext.Context, "Please enter something I can help you with!", 
                    new List<Func<string, string>> { ToHtml.MessageWrapInSpan }));

                conversationData.DidCancel = false;

                await _botService.conversationDataAccessor.SetAsync(stepContext.Context, conversationData);
                await _botService.ConversationState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            }

            
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
