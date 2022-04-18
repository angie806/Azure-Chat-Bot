using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AIAzureChatbot.DataModels;
using AIAzureChatbot.Services;
using AIAzureChatbot.State;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace AIAzureChatbot.Dialogs
{
    public class AccountDocumentsDialog : CancelAndHelpDialog
    {

        private readonly string DocumentModelValue = "" /* [redacted] */;
        private readonly string DocumentTypeEntity = "" /* [redacted] */;
        private readonly string AccountTypeEntity = "" /* [redacted] */;

        private readonly List<string> AccountTypeChoices = new List<string>() { "Standard Account", "IRA Account" };

        private BotStateService _botService;


        public AccountDocumentsDialog(string dialogId, BotStateService botService, RecognitionServices luisService) : base(dialogId, botService, luisService)
        {
            InitializeDialog();
            _botService = botService;
        }

        private void InitializeDialog()
        {
            var waterfallSteps = new WaterfallStep[] {
                ParseAllGivenInformationStepAsync,
                GetAccountTypeAsync,
                EndDialogStepAsync

            };

            AddDialog(new WaterfallDialog($"{nameof(AccountDocumentsDialog)}.waterfall", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(AccountDocumentsDialog)}.accountType"));


            InitialDialogId = $"{nameof(AccountDocumentsDialog)}.waterfall";
        }

        private async Task<DialogTurnResult> ParseAllGivenInformationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = (LuisResult)stepContext.Options;

            var documentModel = new AccountDocumentModel
            {
                //get ira or standard account type
                AccountType = GetEntityModelResolution(luisResult, AccountTypeEntity),
                //get account document
                AccountDocument = GetEntityModelResolution(luisResult, DocumentTypeEntity)
            };


            documentModel.SetIsIra(); //determines if account is an IRA acct or Standard

            //if any account type was specified. 
            if (documentModel.IsIra.HasValue)
            {
                stepContext.Values[DocumentModelValue] = documentModel;

                //the account requested is an IRA
                if (documentModel.IsIra == 1)
                    return await DispatchIraAccountDocumentAsync(stepContext, documentModel, cancellationToken);

                //account is standard
                return await DispatchNonIraAccountDocumentAsync(stepContext, documentModel, cancellationToken);
            }
            else {
                //no account has been supplied

                //if the document specified is an IRA transfer form, but the user did not specify IRA
                if (Common.IraOnlyDocumentTypes.Contains(documentModel.AccountDocument)) {
                    documentModel.AccountType = "ira";
                    stepContext.Values[DocumentModelValue] = documentModel;
                    return await DispatchIraAccountDocumentAsync(stepContext, documentModel, cancellationToken);
                }

                //otherwise, prompt for which account type. 
                var msg = "Which type of account would you like to see ";
                if (string.IsNullOrEmpty(documentModel.AccountDocument))
                {
                    msg += " documents for?";
                }
                else {
                    msg += $" the {documentModel.AccountDocument} application/form for?";
                }

                msg = ChannelFormattingService.FormatSimpleMessage(stepContext.Context, msg);

                stepContext.Values[DocumentModelValue] = documentModel;

                return await stepContext.PromptAsync($"{nameof(AccountDocumentsDialog)}.accountType", new PromptOptions
                {
                    Prompt = MessageFactory.Text(msg),
                    RetryPrompt = MessageFactory.Text(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, "Please select a valid account type")),
                    Choices = ChoiceFactory.ToChoices(AccountTypeChoices)
                }, cancellationToken);
            }               
        }

        private async Task<DialogTurnResult> GetAccountTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (FoundChoice)stepContext.Result;
            var documentModel = (AccountDocumentModel)stepContext.Values[DocumentModelValue];

            //if the user selected an account type, save which type to the model.
            if (result != null)
            {
                switch (result.Index) {
                    case 0:
                        documentModel.AccountType = "standard";
                        documentModel.IsIra = 0;
                        return await DispatchNonIraAccountDocumentAsync(stepContext, documentModel, cancellationToken);
                    case 1:
                        documentModel.AccountType = "ira";
                        documentModel.IsIra = 1;
                        return await DispatchIraAccountDocumentAsync(stepContext, documentModel, cancellationToken);
                }
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> DispatchIraAccountDocumentAsync(WaterfallStepContext stepContext, AccountDocumentModel documentModel, CancellationToken cancellationToken) {
            //if no document was specified for the account, lead the user to the general documents page
            if (string.IsNullOrEmpty(documentModel.AccountDocument))
            {
                var link = ChannelFormattingService.FormatLinkMessageAndSaveToState(stepContext.Context, Common.PIIraDocumentUrl, _botService, cancellationToken);

                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"You can view all Ira-Related Account documents by clicking {link}. " +
                    $"You can also ask for a specific document in the same fashion and I can direct you right to it."));
            }
            //if document was originally specified
            else {
                //get the URL and respond. 
                var link = ChannelFormattingService.FormatLinkMessageAndSaveToState(stepContext.Context, APIService.GetAccountDocumentURL(documentModel), _botService, cancellationToken);

                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"You can view the IRA {documentModel.AccountDocument} document by clicking {link}"));
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> DispatchNonIraAccountDocumentAsync(WaterfallStepContext stepContext, AccountDocumentModel documentModel, CancellationToken cancellationToken)
        {
            var sendGeneralUrl = false;

            //if the user requested a Standard acct transfer form.
            if (Common.IraOnlyDocumentTypes.Contains(documentModel.AccountDocument)) {
                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, 
                    $"Sorry, there is currently not a {documentModel.AccountDocument} document/form for Standard accounts."));
                sendGeneralUrl = true;
            }
            //the user requested a specific document, find the URL and respond. 
            else if (!string.IsNullOrEmpty(documentModel.AccountDocument))
            {
                var link = ChannelFormattingService.FormatLinkMessageAndSaveToState(stepContext.Context, APIService.GetAccountDocumentURL(documentModel), _botService, cancellationToken);

                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"You can view the Standard {documentModel.AccountDocument} " +
                    $"document by clicking {link}"));
            }
            else
            {
                sendGeneralUrl = true;
            }

            //if the user did not specify a document, or the user requested a standard acct. transfer form, lead them to the general document page. 
            if (sendGeneralUrl)
            {
                var link = ChannelFormattingService.FormatLinkMessageAndSaveToState(stepContext.Context, Common.PINonIraDocumentUrl, _botService, cancellationToken);

                await stepContext.Context.SendActivityAsync(ChannelFormattingService.FormatSimpleMessage(stepContext.Context, $"You can view all Standard Account related documents by clicking {link}. " +
                    $"You can also ask for a specific document in the same fashion and I can direct you right to it."));
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        #region Helper methods
        private string GetEntityModelResolution(LuisResult luisResult, string entityType) {
            var entities = luisResult.ConnectedServiceResult.Entities;
            var entity = entities.FirstOrDefault(s => s.Type == entityType);

            if (entity != null)
            {
                return Common.GetResolutionFromEntity(entity);
            }

            return null;
        }
        #endregion
    }
}
