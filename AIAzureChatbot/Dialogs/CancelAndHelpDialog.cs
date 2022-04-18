// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIAzureChatbot.Services;
using AIAzureChatbot.State;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace AIAzureChatbot.Dialogs
{
    public class CancelAndHelpDialog : ComponentDialog
    {
        private readonly BotStateService _botService;
        private readonly RecognitionServices _luisService;

        public CancelAndHelpDialog(string id, BotStateService botService, RecognitionServices luisService)
            : base(id)
        {
            _botService = botService;
            _luisService = luisService;
        }

        //method is triggered right before the a dialog is about to begin
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        //method is triggered right before a dialog is being continued (i.e. second step of a waterfall dialog)
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        //this method checks for user interruptions. specifically returns a non-null value if the user says cancel or help
        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var conversationData = await _botService.conversationDataAccessor.GetAsync(innerDc.Context, () => new ConversationData());

            //if the current activity is a message
            if (innerDc.Context.Activity.Type == ActivityTypes.Message) {

                var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                //send message to LUIS
                var recognizerResult = await _luisService.Dispatch.RecognizeAsync(innerDc.Context, cancellationToken);
                var topIntent = recognizerResult.GetTopScoringIntent();

                if (topIntent.intent == "luis_intent")
                {
                    var luisResult = recognizerResult.Properties["luisResult"] as LuisResult;

                    switch (luisResult.ConnectedServiceResult.TopScoringIntent.Intent)
                    {
                        case "Utilities.Help":

                            await innerDc.Context.SendActivityAsync(ChannelFormattingService.FormatMessages(innerDc.Context, $"Showing help...", 
                                new List<Func<string, string>> { ToHtml.MessageWrapInSpan }), cancellationToken: cancellationToken);
                            return await innerDc.CancelAllDialogsAsync();

                        case "Utilities.Cancel":
                            await innerDc.Context.SendActivityAsync(ChannelFormattingService.FormatMessages(innerDc.Context, $"Alright!",
                                new List<Func<string, string>> { ToHtml.MessageWrapInSpan }), cancellationToken: cancellationToken);

                            await _botService.conversationDataAccessor.SetAsync(innerDc.Context, conversationData);
                            await _botService.ConversationState.SaveChangesAsync(innerDc.Context, false, cancellationToken);

                            return await innerDc.CancelAllDialogsAsync();
                    }
                }
            }

            return null;
        }
    }
}
