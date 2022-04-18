using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AIAzureChatbot.Services;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;
using AIAzureChatbot.DataModels;
using Microsoft.Bot.Builder;
using System.Globalization;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace AIAzureChatbot.Dialogs
{
    public class FundSpecificAttributesDialog : CancelAndHelpDialog
    {

        private readonly string PIFundName = "" /* [redacted] */;
        private readonly string PIFundSharesType = "" /* [redacted] */;

        private readonly string FundAttributesModelValue = "" /* [redacted] */;

        private Dictionary<string, string> AttributeTypeToDialogId;

        private BotStateService _botService;
        private RecognitionServices _luisService;

        public static int loopCounter = 0;


        /*
         *   IMPORTANT:
         *          this dialog is structured in a way that is different than the others. there are two waterfall dialogs. 
         *          The first outer waterfall dialog is to parse the luisResult to figure out which type of attributes were requested
         *              these attributes are intended to be grouped by FundBasic attributes, FundPerformance attributes, etc. 
         *              Currently, only FundBasic is implemented.
         *              
         *          The second inner waterfall dialog is to dispatch different dialogs based on the attributes requested. If FundBasic and FundPerformance
         *          attributes were requested, then it will first begin the FundBasicAttributeDialog, and once it ends, it will begin the FundPerformanceAttributeDialog (when added)
         * 
         */
        public FundSpecificAttributesDialog(string dialogId, BotStateService botService, RecognitionServices luisService) : base(dialogId, botService, luisService)
        {
            _botService = botService;
            _luisService = luisService;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            loopCounter = 0;

            //Initialize the first, outer waterfall dialog
            var mainWaterfallSteps = new WaterfallStep[] {
                ParseGivenParametersAsync,
                DispatchFundAttributesLoopAsync,
                EndMainWaterfallStepAsync
            };
            AddDialog(new WaterfallDialog($"{nameof(FundSpecificAttributesDialog)}.mainWaterfall", mainWaterfallSteps));

            //Initialize the second, inner waterfall dialog
            var attributeLoopSteps = new WaterfallStep[] {
                InitializeAttributeLoopAsync,
                ContinueOrEndAttributeLoopAsync
            };
            AddDialog(new WaterfallDialog($"{nameof(FundSpecificAttributesDialog)}.attrLoopDialog", attributeLoopSteps));

            //Add FundBasicAttribute Dialog. This will trigger if the user requested any fund basic attributes
            AddDialog(new FundBasicsAttributeDialog($"{nameof(FundSpecificAttributesDialog)}.fundBasics", _botService, _luisService));

            //Dictionary that maps the LUIS Entity Type to the Dialog. These entity types will be figured out in the first, outer waterfall step. 
            AttributeTypeToDialogId = new Dictionary<string, string> {
                /* [redacted] */
            };
            
            //Neccessary Text and prompts to ask for fund name if not given.
            AddDialog(new TextPrompt($"{nameof(FundSpecificAttributesDialog)}.askFundName", VerifyValidFundNameSelected));

            //AddDialog(new ChoicePrompt($"{nameof(FundSpecificAttributesDialog)}.askSharesType"));


            InitialDialogId = $"{nameof(FundSpecificAttributesDialog)}.mainWaterfall";
        }

        private async Task<DialogTurnResult> ParseGivenParametersAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var attrModel = new FundAttributesModel();
            var luisResult = (LuisResult)stepContext.Options;

            /*find the attribute entity types that were recognized. 
             *      i.e. If I send "I want the PARMX expense ratio", this step will 
             *      find that the user requested FundBasicAttributeType and save it within the FundAttributesModel
             */   
            FindFundNameAndAttributeEntities(attrModel, luisResult);

            //save the model
            stepContext.Values[FundAttributesModelValue] = attrModel;

            //Prompt for fund name by giving a list of choices. This makes it so the user doesn't get prompted with an error if he/she tries to type the fund themselves
            if (string.IsNullOrEmpty(attrModel.PIFundName)) {
                var SuggestedActions = SuggestedActionGenerator.GenerateSuggestedActions(Common.PIFundNames);
                var reply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Which fund would you like to see your requested attributes for?"));
                reply.SuggestedActions = SuggestedActions;

                var retryReply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Please select a valid fund to view its attributes"));
                retryReply.SuggestedActions = SuggestedActions;

                return await stepContext.PromptAsync($"{nameof(FundSpecificAttributesDialog)}.askFundName", new PromptOptions
                {
                    Prompt = reply,
                    RetryPrompt = retryReply
                }, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> DispatchFundAttributesLoopAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            var fundName = ((string)stepContext.Result);
            var attrModel = (FundAttributesModel)stepContext.Values[FundAttributesModelValue];

            //Parse the fund name given from the previous step (if any) and save it to attributeModel
            if (fundName != null)
            {
                fundName = fundName.Trim().ToLower();

                //if fund name was given as Mid cap fund, we must add PI to the front for usage with the API service
                if (fundName != Common.PIFundNames[Common.PIFundIndexInNamesList].ToLower())
                {
                    fundName = "PI " + fundName;
                }

                attrModel.PIFundName = fundName;
                stepContext.Values[FundAttributesModelValue] = attrModel;
            }

            //send the LuisResult and AttributeModel to the second, inner waterfall dialog
            List<object> options = new List<object> { (LuisResult)stepContext.Options, attrModel };
            return await stepContext.BeginDialogAsync($"{nameof(FundSpecificAttributesDialog)}.attrLoopDialog", options, cancellationToken);

        }

        private async Task<DialogTurnResult> EndMainWaterfallStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        //Start of Attribute Loop dialog
        private async Task<DialogTurnResult> InitializeAttributeLoopAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            var attrModel = ((FundAttributesModel)((List<Object>)stepContext.Options)[1]);
            var dict = attrModel.GivenAttributesDictionary;

            //Loops through the grouped fund attributes requested (i.e. FundBasic or FundPerformance) and begins the related dialog
            foreach ((string key, bool value) in dict) {
                // value == true if the dialog has not been used
                if (value) {
                    dict[key] = false;
                    LuisResult luisResult = (LuisResult)((List<Object>)stepContext.Options)[0];

                    //send the LuisResult and AttributeModel to the new child dialog
                    return await stepContext.BeginDialogAsync(AttributeTypeToDialogId[key], new List<Object> { luisResult, attrModel }, cancellationToken);
                }
            }

            // no more dialogs to begin
            return await stepContext.NextAsync(false, cancellationToken);

        }

        private async Task<DialogTurnResult> ContinueOrEndAttributeLoopAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //if dialog just ended, we need to restart the waterfall dialog to check for more. 
            if ((bool)stepContext.Result)
            {
                
                return await stepContext.ReplaceDialogAsync($"{nameof(FundSpecificAttributesDialog)}.attrLoopDialog", stepContext.Options, cancellationToken);
            }
            //if no more dialogs
            else {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }


        #region Validators
        //verifies that the fund name given when asked by the bot is valid. 
        public Task<bool> VerifyValidFundNameSelected(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var names = Common.PIFundNames.ToList().ConvertAll(s => s.ToLowerInvariant());

                valid = names.Contains(promptContext.Recognized.Value.Trim().ToLower());
            }

            return Task.FromResult(valid);
        }
        #endregion

        #region Helper methods

        private void FindFundNameAndAttributeEntities(FundAttributesModel attrModel, LuisResult luisResult) {
            var attrDict = attrModel.GivenAttributesDictionary;

            foreach (var entity in luisResult.ConnectedServiceResult.Entities) {

                //found the fund name given
                if (entity.Type == PIFundName)
                {
                    var fund = Common.GetResolutionFromEntity(entity);
                    
                    //if fund is a ticker
                    if (fund.Trim().Length == 5) {
                        attrModel.PIFundName = Common.tickersToFundName[fund.Trim().ToUpper()];

                        //figure out which type of shares from ticker
                        if (Common.investFundSymbols.Values.Contains(fund.Trim().ToUpper()))
                        {
                            attrModel.PISharesType = 1;
                        }
                        else {
                            attrModel.PISharesType = 2;
                        }
                    } else {
                        attrModel.PIFundName = fund;
                    }
                }
                //found the shares type given
                else if (entity.Type == PIFundSharesType) {
                    attrModel.PISharesType = Common.GetResolutionFromEntity(entity) == "investor shares" ? 1 : 2;
                }
                //found an entity type group (i.e. FundBasicAttributeType)
                else if (attrDict.ContainsKey(entity.Type))
                {
                    attrDict[entity.Type] = true;
                }
            }
        }

        #endregion
    }
}
