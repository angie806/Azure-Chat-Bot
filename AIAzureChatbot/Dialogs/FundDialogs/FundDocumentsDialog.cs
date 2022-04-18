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
    public class FundDocumentsDialog : CancelAndHelpDialog
    {

        private readonly string FundDocumentModelValue = "" /* [redacted] */;

        private readonly string DateTimeRange = "" /* [redacted] */;
        private readonly string DocumentType = "" /* [redacted] */;
        private readonly string FundName = "" /* [redacted] */;
        private readonly string QuarterDateTime = "" /* [redacted] */;

        private BotStateService _botService;

        public FundDocumentsDialog(string dialogId, BotStateService botService, RecognitionServices luisService) : base(dialogId, botService, luisService)
        {
            InitializeDialog();
            _botService = botService;
        }

        private void InitializeDialog()
        {
            var waterfallSteps = new WaterfallStep[] {
                ParseGivenParametersAsync,
                GetFundNameStepAsync,
                GetDocumentAssociatedWithFundNameStepAsync,
                GetFundDocumentURLStepAsync,
            };

            AddDialog(new WaterfallDialog($"{nameof(FundDocumentsDialog)}.waterfall", waterfallSteps));

            //text prompt dialog to select fund name, if user doesn't give it
            AddDialog(new TextPrompt($"{nameof(FundDocumentsDialog)}.askFundName", VerifyValidFundNameSelected));

            //choice prompt dialog to select fund document, if fund was given but not document
            AddDialog(new ChoicePrompt($"{nameof(FundDocumentsDialog)}.askFundDocument"));


            /*
             * Note:
             *      The reason why I used a choice/text prompt is because I've experienced that if you have more than 3 choices, using a choice prompt
             *      concatenates the choices to where the user is unable to see the choices in a nice format. therefore, if there are >3 choices, i use a text prompt
             *      and simply add SuggestedActions. 
             *      
             *      Also, choice prompts do its own validation. if the user types out the choice and it does not match the choices, case insentitive, it will promppt to retry.
             */

            InitialDialogId = $"{nameof(FundDocumentsDialog)}.waterfall";
        }

        private async Task<DialogTurnResult> ParseGivenParametersAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = (LuisResult)stepContext.Options;
            var documentModel = new FundDocumentModel();

            //Get all necessary entities to perform query for fund documents
            var parsedDate = GetDateTimeEntity(luisResult); //try get year
            documentModel.SetDate(parsedDate);
            documentModel.SetQuarter(GetEntityModelResolution(luisResult, QuarterDateTime)); //try get quarter
            documentModel.FundName = GetEntityModelResolution(luisResult, FundName); //try get fund name
            documentModel.FundDocument = GetEntityModelResolution(luisResult, DocumentType); //try get fund document

            //if no document and no fund was specified, direct user to fund document page
            if (string.IsNullOrEmpty(documentModel.FundDocument) && string.IsNullOrEmpty(documentModel.FundName)) {
                var link = ChannelFormattingService.FormatLinkMessageAndSaveToState(stepContext.Context, Common.FundDocumentsURL, _botService, cancellationToken);

                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, 
                    $"You can view all fund documents by clicking {link}"));

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var needsFund = false;

            //if no fund was given, but the document requires a fund to be specified
            if (string.IsNullOrEmpty(documentModel.FundName) && Common.DocumentsRequireFundName.Contains(documentModel.FundDocument))
                needsFund = true;

            stepContext.Values[FundDocumentModelValue] = documentModel;
            return await stepContext.NextAsync(needsFund, cancellationToken);
        }

        private async Task<DialogTurnResult> GetFundNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            var documentModel = (FundDocumentModel)stepContext.Values[FundDocumentModelValue];

            //if no fund is needed
            if (!((bool)stepContext.Result))
                return await stepContext.NextAsync(null, cancellationToken);

            //generate choices with the list of funds
            var SuggestedActions = SuggestedActionGenerator.GenerateSuggestedActions(Common.PIFundNames);
            var reply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Which fund would you like to see the {documentModel.FundDocument} for?"));
            reply.SuggestedActions = SuggestedActions;

            var retryReply = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Please select a valid fund to view its {documentModel.FundDocument}"));
            retryReply.SuggestedActions = SuggestedActions;

            //ask for fund name
            return await stepContext.PromptAsync($"{nameof(FundDocumentsDialog)}.askFundName", new PromptOptions
            {
                Prompt = reply,
                RetryPrompt = retryReply
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> GetDocumentAssociatedWithFundNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            var fundName = ((string)stepContext.Result);
            var documentModel = (FundDocumentModel)stepContext.Values[FundDocumentModelValue];

            //get response from user, if any, and save it to the model
            if (fundName != null)
            {
                fundName = fundName.Trim().ToLower();

                if (fundName != Common.PIFundNames[Common.PIFundIndexInNamesList].ToLower())
                {
                    fundName = "PI " + fundName;
                }

                documentModel.FundName = fundName;
                stepContext.Values[FundDocumentModelValue] = documentModel;
            }

            if (!string.IsNullOrEmpty(documentModel.FundDocument)) {
                //wants general PI Fund document   i.e annual report
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var documentChoices = Common.DocumentsRequireFundName.ToList().ConvertAll(s => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s));

            //if no document was specified, but a fund was, ask to select which document. 
            return await stepContext.PromptAsync($"{nameof(FundDocumentsDialog)}.askFundDocument", new PromptOptions {
                Prompt = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Which {CultureInfo.InvariantCulture.TextInfo.ToTitleCase(documentModel.FundName)} document would you like to see?")),
                RetryPrompt = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"Please select a valid document option")),
                Choices = ChoiceFactory.ToChoices(documentChoices)
            }, cancellationToken);

        }

        private async Task<DialogTurnResult> GetFundDocumentURLStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            
            var documentModel = (FundDocumentModel)stepContext.Values[FundDocumentModelValue];
            var result = (FoundChoice)stepContext.Result;


            if (result != null) {
                documentModel.FundDocument = result.Value.Trim().ToLower();
            }

            var fundDocumentModel = APIService.GetFundDocumentUrl(documentModel);

            //no document was found. 
            if (fundDocumentModel == null)
            {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "The requested document is not available."));
            }
            else {
                //get the fund document's attributes and send a formatted response. 
                var month = fundDocumentModel.Month == default ? "" : new DateTime(2000, fundDocumentModel.Month, 1).ToString("MMM", CultureInfo.InvariantCulture);
                var year = fundDocumentModel.Year == default ? "" : $"{fundDocumentModel.Year}";
                var quarter = fundDocumentModel.Quarter == default ? "" : $"Q{fundDocumentModel.Quarter}";
                var parnFund = fundDocumentModel.FundName == null ? "" : $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fundDocumentModel.FundName)}";
                var url = fundDocumentModel.HasKenticoURL ? APIService.domain + fundDocumentModel.DocumentURL : fundDocumentModel.DocumentURL;

                var link = ChannelFormattingService.FormatLinkMessageAndSaveToState(stepContext.Context, url, _botService, cancellationToken);

                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"You can view the {month} {year} {quarter} {parnFund} {documentModel.FundDocument} " +
                    $"by clicking {link}"));

            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #region Validators
        //verifies that the fund selected by the user is valid.
        public Task<bool> VerifyValidFundNameSelected(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded) {
                var names = Common.PIFundNames.ToList().ConvertAll(s => s.ToLowerInvariant());

                valid = names.Contains(promptContext.Recognized.Value.Trim().ToLower());
            }

            return Task.FromResult(valid);
        }
         #endregion

        #region Helper methods
        private bool IsValidDatetimeEntity(string entityValue) {
            //we want to discard entities that caught both the quarter and year in the same entity. this 
            //allows us to separate the quarter and year into different entities. 
            // i.e. we want two separate entities like "2019" and "Q1" but not one combined entity "Q1 2019"

            return !Regex.IsMatch(entityValue, @"^(?!.*year).*(quarter|q\d+).*$");   //false if entity only contains a quarter value, true if contains only year or year & quarter
        }

        private string GetEntityModelResolution(LuisResult luisResult, string entityType)
        {
            var entities = luisResult.ConnectedServiceResult.Entities;
            var entity = entities.FirstOrDefault(s => s.Type == entityType);

            if (entity != null)
            {
                var value = Common.GetResolutionFromEntity(entity);

                //if the entity is a ticker, get the associated fund name
                if (entityType == FundName && value.Trim().Length == 5) {
                    value = Common.tickersToFundName[value.Trim().ToUpper()];
                } 

                return value;
            }

            return null;
        }

        private DateTime GetDateTimeEntity(LuisResult luisResult) {
            var entities = luisResult.ConnectedServiceResult.Entities;

            //find entities that only contain the year (i.e. 2019)
            var entity = entities.FirstOrDefault(s => (s.Type == DateTimeRange && IsValidDatetimeEntity(s.Entity)));

            if (entity != null)
            {
                //the datetime.range entity is structured in a weird way by LUIS. its like resolution: { value: { start: "2019-1-1", end... }}}
                // this is why we can't simply take the "value" object. we have to dig further. 

                var resolution = (JObject)entity.AdditionalProperties["resolution"];
                var values = resolution["values"].First().Value<JObject>();
                return DateTime.Parse(values.Value<string>("start"));
            }
            else {
                return default;
            }
        }
        #endregion
    }
}
