using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class FundHoldingsDialog : CancelAndHelpDialog
    {
        private const string FundName = "" /* [redacted] */;
        private const string CompanyName = "" /* [redacted] */;
        private const string HoldingAttribute = "" /* [redacted] */;

        private readonly string FundHoldingModelValue = "" /* [redacted] */;

        public FundHoldingsDialog(string dialogId, BotStateService botService, RecognitionServices luisService) : base(dialogId, botService, luisService)
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            var waterfallSteps = new WaterfallStep[] {
                ParseGivenParametersAsync,
                GetHoldingsStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(FundHoldingsDialog)}.waterfall", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(FundHoldingsDialog)}.fundName", VerifyValidFundNameSelected));

            InitialDialogId = $"{nameof(FundHoldingsDialog)}.waterfall";
        }

        private async Task<DialogTurnResult> ParseGivenParametersAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = (LuisResult)stepContext.Options;

            var holdingModel = new FundHoldingsModel();
            holdingModel.HoldingAttributes = new List<string>();
            holdingModel.FundName = new List<string>();

            //get the fund names, company, and (optional) holding attributes requested
            GetEntityModelResolution(luisResult, holdingModel);

            stepContext.Values[FundHoldingModelValue] = holdingModel;

            // no company was recognized
            if (string.IsNullOrEmpty(holdingModel.Company)) {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "Please specify a company to see holding information."));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            //if no funds were selected, prompt user to select a fund or select all funds. 
            else if (holdingModel.FundName.Count == 0) {
                var choices = Common.PIFundNames.ToList();
                choices.Insert(0, "All funds");
                var suggestedActions = SuggestedActionGenerator.GenerateSuggestedActions(choices);

                var reply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "Which fund would you like to see holding information for?"));
                var retryReply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "Please select a valid choice"));
                reply.SuggestedActions = suggestedActions;
                retryReply.SuggestedActions = suggestedActions;

                return await stepContext.PromptAsync($"{nameof(FundHoldingsDialog)}.fundName", new PromptOptions {
                    Prompt = reply,
                    RetryPrompt = retryReply
                }, cancellationToken);

            }


            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> GetHoldingsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            var fundName = ((string)stepContext.Result);
            var holdingModel = (FundHoldingsModel)stepContext.Values[FundHoldingModelValue];

            //if a fund name was selected from the previous step, save it to the model
            if (fundName != null)
            {
                fundName = fundName.Trim().ToLower();

                if (fundName == "all funds") {
                    holdingModel.AllFunds = true;
                }
                else if (fundName != Common.PIFundNames[Common.PIFundIndexInNamesList].ToLower())
                {
                    fundName = "PI " + fundName;
                }  

                holdingModel.FundName.Add(fundName);
                stepContext.Values[FundHoldingModelValue] = holdingModel;
            }

            //get all holdings
            var allHoldings = await APIService.GetFundHoldingsAttributes(holdingModel);

            //if none were found,the company is not a holding in the requested funds.
            if (allHoldings == null) {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(holdingModel.Company)} is not a holding in the requested fund"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            //format the holdings in a header -> subititle -> list
            var response = ChannelFormattingService.FormatHeaderSubtitleAndList(stepContext.Context,
                 $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(holdingModel.Company)}" + " holding information:", allHoldings);

            await stepContext.Context.SendActivityAsync(response, null, null, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #region Validators
        //verify that the fund selected by the user is either a valid PI fund or "All funds"
        public Task<bool> VerifyValidFundNameSelected(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var possibleChoices = Common.PIFundNames.ToList();
                possibleChoices.Add("All funds");
                var names = possibleChoices.ConvertAll(s => s.ToLowerInvariant());

                valid = names.Contains(promptContext.Recognized.Value.Trim().ToLower());
            }

            return Task.FromResult(valid);
        }
        #endregion

        #region Helper methods
        //get all the LUIS recognized entities.
        private void GetEntityModelResolution(LuisResult luisResult, FundHoldingsModel holdingModel)
        {
            var entities = luisResult.ConnectedServiceResult.Entities;

            if (entities != null)
            {
                foreach (EntityModel entity in entities) {
                    switch (entity.Type) {
                        case FundName:
                            var fund = Common.GetResolutionFromEntity(entity);


                            if (fund == "any fund")
                            {
                                holdingModel.AllFunds = true;
                                holdingModel.FundName.Add(fund);
                            }
                            else if (fund.Trim().Length == 5)
                            {
                                //is a fund ticker
                                holdingModel.FundName.Add(Common.tickersToFundName[fund.Trim().ToUpper()]);
                            }
                            else {
                                holdingModel.FundName.Add(fund);
                            }

                            break;
                        case CompanyName:
                            holdingModel.Company = entity.Entity;
                            break;
                        //found a holding attribute. i.e. # of shares or Market value. 
                        case HoldingAttribute:
                            
                            holdingModel.HoldingAttributes.Add(Common.GetResolutionFromEntity(entity));
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
