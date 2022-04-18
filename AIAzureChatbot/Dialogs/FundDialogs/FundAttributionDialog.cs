using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIAzureChatbot.DataModels;
using AIAzureChatbot.Services;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;

namespace AIAzureChatbot.Dialogs.FundDialogs
{
    public class FundAttributionDialog : CancelAndHelpDialog
    {
        private const string FundName = "" /* [redacted] */;
        private const string AttributionType = "" /* [redacted] */;
        private const string AttributionChars = "" /* [redacted] */;

        private const string FundAttributionModelValue = "" /* [redacted] */;

        public FundAttributionDialog(string dialogId, BotStateService botService, RecognitionServices luisService) : base(dialogId, botService, luisService)
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            var waterfallSteps = new WaterfallStep[] {
                ParseGivenParametersAsync,
                GetAttributionsAsync,
                
            };

            AddDialog(new WaterfallDialog($"{nameof(FundAttributionDialog)}.waterfall", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(FundAttributionDialog)}.fundName", VerifyValidFundNameSelected));

            InitialDialogId = $"{nameof(FundAttributionDialog)}.waterfall";
        }

        private async Task<DialogTurnResult> ParseGivenParametersAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = (LuisResult)stepContext.Options;
            var attributionModel = new FundAttributionModel();

            //find all entities found by LUIS and save it to the model
            GetEntityResolutions(luisResult, attributionModel);

            //if the user requested "any fund"
            if (attributionModel.FundName == "any fund") {
                attributionModel.FundName = null;
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "You must select a fund to view top contributors/detractors"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            stepContext.Values[FundAttributionModelValue] = attributionModel;

            //if no fund name is given, we need to ask the user
            if (string.IsNullOrEmpty(attributionModel.FundName)) {
                var choices = Common.PIFundNames.ToList();
                var suggestedActions = SuggestedActionGenerator.GenerateSuggestedActions(choices);

                var placeholder = attributionModel.ContributorsOrDetractors == 0 ? "top contributors" : "top detractors";

                var reply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Which fund would you like to see {placeholder} for?"));
                var retryReply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "Please select a valid choice"));
                reply.SuggestedActions = suggestedActions;
                retryReply.SuggestedActions = suggestedActions;

                return await stepContext.PromptAsync($"{nameof(FundAttributionDialog)}.fundName", new PromptOptions
                {
                    Prompt = reply,
                    RetryPrompt = retryReply
                }, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        public async Task<DialogTurnResult> GetAttributionsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            var fundName = (string)stepContext.Result;
            var attributionModel = (FundAttributionModel)stepContext.Values[FundAttributionModelValue];

            //save given fund name (if any) to the model
            if (fundName != null)
            {
                fundName = fundName.Trim().ToLower();

                //if selected choice was "Mid cap fund" etc, we must add PI to the front. (does not apply to PI Fund)
                if (fundName != Common.PIFundNames[Common.PIFundIndexInNamesList].ToLower())
                {
                    fundName = "PI " + fundName;
                }

                attributionModel.FundName = fundName;
            }

            //get fund contributors or detractors
            var fundAttributions = APIService.GetFundAttributions(attributionModel).Result;

            //format it in a header -> subtitle -> data 
            var placeholder = attributionModel.ContributorsOrDetractors == 0 ? "contributors" : "detractors";
            var response = ChannelFormattingService.FormatHeaderSubtitleAndList(stepContext.Context,
                 $"{attributionModel.FundTicker} top {placeholder}:", fundAttributions);

            await stepContext.Context.SendActivityAsync(response, null, null, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #region Validators
        public Task<bool> VerifyValidFundNameSelected(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var possibleChoices = Common.PIFundNames.ToList();
                var names = possibleChoices.ConvertAll(s => s.ToLowerInvariant());

                valid = names.Contains(promptContext.Recognized.Value.Trim().ToLower());
            }

            return Task.FromResult(valid);
        }
        #endregion

        #region Helper methods
        public void GetEntityResolutions(LuisResult luisResult, FundAttributionModel attrModel) {
            var entities = luisResult.ConnectedServiceResult.Entities;
            if (entities != null) {
                foreach (EntityModel entity in entities) {
                    switch (entity.Type) {
                        //found fund name
                        case FundName:
                            var fund = Common.GetResolutionFromEntity(entity);
                            //given fund name is a ticker
                            if (fund.Trim().Length == 5)
                            {
                                attrModel.FundName = Common.tickersToFundName[fund.Trim().ToUpper()];
                            }
                            else {
                                attrModel.FundName = fund;
                            }

                            break;
                        //found contributor or detractor
                        case AttributionType:
                            var attrType = Common.GetResolutionFromEntity(entity);
                            if (attrType == "contributor")
                            {
                                attrModel.ContributorsOrDetractors = 0;
                            }
                            else {
                                attrModel.ContributorsOrDetractors = 1;
                            }

                            break;
                        //found "return of holding" or "contribution to portfolio return"
                        case AttributionChars:
                            attrModel.AttributionChar = Common.GetResolutionFromEntity(entity);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        #endregion
    }
}
