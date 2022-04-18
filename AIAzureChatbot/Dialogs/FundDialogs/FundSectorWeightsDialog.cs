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
    public class FundSectorWeightsDialog : CancelAndHelpDialog
    {
        private const string FundName = "" /* [redacted] */;
        private const string SectorType = "" /* [redacted] */;

        private readonly string FundSectorModelValue = "" /* [redacted] */;


        public FundSectorWeightsDialog(string dialogId, BotStateService botService, RecognitionServices luisService) : base(dialogId, botService, luisService)
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            var waterfallSteps = new WaterfallStep[] {
                ParseGivenParametersAsync,
                GetSectorWeightingsAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(FundSectorWeightsDialog)}.waterfall", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(FundSectorWeightsDialog)}.fundName", VerifyValidFundNameSelected));

            InitialDialogId = $"{nameof(FundSectorWeightsDialog)}.waterfall";
        }

        private async Task<DialogTurnResult> ParseGivenParametersAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = (LuisResult)stepContext.Options;

            var sectorModel = new FundSectorWeightsModel();
            sectorModel.Funds = new List<string>();

            //find fund names and sector
            GetEntityResolutions(luisResult, sectorModel);

            stepContext.Values[FundSectorModelValue] = sectorModel;


            //if no sector was given
            if (string.IsNullOrEmpty(sectorModel.Sector))
            {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "Please select a valid sector to view weighting information"));
                return await stepContext.EndDialogAsync(null, cancellationToken);

            }
            //if no funds were given, prompt the user to select a fund. 
            else if (sectorModel.Funds.Count == 0) {
                var choices = Common.PIFundNames.ToList();
                choices.Insert(0, "All funds");
                var suggestedActions = SuggestedActionGenerator.GenerateSuggestedActions(choices);

                var reply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Which fund would you like to see " +
                    $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(sectorModel.Sector)} sector weightings for?"));
                var retryReply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "Please select a valid choice"));
                reply.SuggestedActions = suggestedActions;
                retryReply.SuggestedActions = suggestedActions;

                return await stepContext.PromptAsync($"{nameof(FundSectorWeightsDialog)}.fundName", new PromptOptions
                {
                    Prompt = reply,
                    RetryPrompt = retryReply
                }, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);

        }

        private async Task<DialogTurnResult> GetSectorWeightingsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fundName = (string)stepContext.Result;
            var sectorModel = (FundSectorWeightsModel)stepContext.Values[FundSectorModelValue];

            //if a fund was selected from the previous step, save it to the model. 
            if (fundName != null)
            {
                fundName = fundName.Trim().ToLower();

                if (fundName != "all funds" && fundName != Common.PIFundNames[Common.PIFundIndexInNamesList].ToLower())
                {
                    fundName = "PI " + fundName;
                }

                sectorModel.AddFund(fundName);
            }

            //if the funds requested contains the fixed income fund, tell the user that no sector weightings exist and remove it. 
            if (sectorModel.Funds.Any(s => s == "PRFIX"))
            {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "The PI Fixed Income Fund does not contain sector weightings"));
                sectorModel.Funds.RemoveAll(s => s == "PRFIX");
                stepContext.Values[FundSectorModelValue] = sectorModel;

                //if the fixed income fund was the only fund requested, end the dialog. 
                if (sectorModel.Funds.Count == 0) {
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }

            //get the fund sector weights
            var fundSectorWeights = APIService.GetFundSectorWeights(sectorModel).Result;

            //format the sector weights in header -> subtitles -> list. 
            var response = ChannelFormattingService.FormatHeaderSubtitleAndList(stepContext.Context,
                $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(sectorModel.Sector)} sector weights", fundSectorWeights);

            await stepContext.Context.SendActivityAsync(response, null, null, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #region Validators
        //verify that the fund selected by the user is a valid PI fund or is "All funds"
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
        public void GetEntityResolutions(LuisResult luisResult, FundSectorWeightsModel sectorModel)
        {
            var entities = luisResult.ConnectedServiceResult.Entities;
            if (entities != null)
            {
                foreach (EntityModel entity in entities)
                {
                    switch (entity.Type)
                    {
                        //found a fund 
                        case FundName:
                            var fund = Common.GetResolutionFromEntity(entity);
                            if (fund.Trim().Length == 5)
                            {
                                sectorModel.AddFund(Common.tickersToFundName[fund.Trim().ToUpper()]);
                            }
                            else
                            {
                                sectorModel.AddFund(fund);
                            }
                            break;
                        //found a sector
                        case SectorType:
                            sectorModel.Sector = Common.GetResolutionFromEntity(entity);
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
