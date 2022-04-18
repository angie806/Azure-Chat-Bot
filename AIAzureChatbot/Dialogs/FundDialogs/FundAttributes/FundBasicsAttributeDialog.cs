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
    public class FundBasicsAttributeDialog : CancelAndHelpDialog
    {
        private readonly string EntityType = "" /* [redacted] */;

        public FundBasicsAttributeDialog(string dialogId, BotStateService botService, RecognitionServices luisService) : base(dialogId, botService, luisService)
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            var waterfallSteps = new WaterfallStep[] {
                ParseGivenParametersAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(FundBasicsAttributeDialog)}.waterfall", waterfallSteps));

            InitialDialogId = $"{nameof(FundBasicsAttributeDialog)}.waterfall";
        }

        private async Task<DialogTurnResult> ParseGivenParametersAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            //get luisResult containing all entities and the attribute model from the options sent from the parent dialog
            LuisResult luisResult = (LuisResult)((List<object>)stepContext.Options)[0];
            FundAttributesModel attrModel = (FundAttributesModel)((List<object>)stepContext.Options)[1];

            attrModel.FundBasicAttributes = new List<string>();
           
            // store recognized entities into the Attribute model
            ParseEntities(attrModel, luisResult);

            //get fund basic attributes requested
            Dictionary<string, Dictionary<string, string>> basicAttributes = await APIService.GetFundBasicsAttributes(attrModel);

            //send response in a subtitle > list format
            var message = ChannelFormattingService.FormatSubtitleAndList(stepContext.Context, basicAttributes);

            await stepContext.Context.SendActivityAsync(message, null, null, cancellationToken);

            return await stepContext.EndDialogAsync(true, cancellationToken);
        }

        #region Helper methods
        private void ParseEntities(FundAttributesModel attrModel, LuisResult luisResult)
        {
            var entityList = attrModel.FundBasicAttributes;

            // find all entities that were detected as Fund Basic attributes
            var entities = luisResult.ConnectedServiceResult.Entities.Where(s => s.Type == EntityType);

            foreach (var entity in entities) {
                //get the normalized value
                var value = Common.GetResolutionFromEntity(entity);

                //add to list if it is not there (NEEDED)
                if (!entityList.Contains(value)) {
                    entityList.Add(value);
                }
            }
        }
        #endregion
    }
}
