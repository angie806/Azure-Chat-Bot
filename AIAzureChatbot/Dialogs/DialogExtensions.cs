using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace AIAzureChatbot.Dialogs
{
   
    public static class DialogExtensions
    {
        //necessary actions when starting a new dialog. I didn't necessarily write this extension myself. it was given by microsoft but I tried to 
        //understand it as much as possible
        public static async Task Run(this Dialog dialog, ITurnContext turnContext,
            IStatePropertyAccessor<DialogState> accessor, CancellationToken cancellationToken)
        {
            //get the current dialog set (stack frames of dialogs) and add the current dialog you want to start. 
            var dialogSet = new DialogSet(accessor); //this set should be empty. 
            dialogSet.Add(dialog);

            //create a dialog context. i believe this is used to mantain the position of where the user is in a waterfall dialog. 
            var dialogContext = await dialogSet.CreateContextAsync(turnContext, cancellationToken);

            //i believe this is for if we need to continue a dialog, i.e. in the middle of a waterfall
            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

            //if we are not waiting for a dialog to finish, start the requested dialog. 
            if (results.Status == DialogTurnStatus.Empty) {
                await dialogContext.BeginDialogAsync(dialog.Id, null, cancellationToken);
            }
        }
    }
}
