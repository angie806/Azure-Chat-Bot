using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIAzureChatbot.State;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace AIAzureChatbot.Services
{
    public class BotStateService
    {
        public UserState UserState { get; set; }
        public ConversationState ConversationState { get; set; }
        public DialogState DialogState { get; set; }

        //unique IDs to be able to identify which User/Conversation/Dialog state to retrieve. 
        public static string UserProfileId { get; } = /* [redacted] */;
        public static string ConversationDataId { get; } = /* [redacted] */;
        public static string DialogStateId { get; } = /* [redacted] */;

        //These are accessors to get the current User state, conversationState, and DialogState. 
        public IStatePropertyAccessor<ConversationData> conversationDataAccessor { get; set; }
        public IStatePropertyAccessor<UserProfile> profileStateAccessor { get; set; }
        public IStatePropertyAccessor<DialogState> dialogStateAccessor { get; set; }

        public BotStateService(UserState userState, ConversationState conversationState, DialogState dialogState) {
            UserState = userState;
            ConversationState = conversationState;
            DialogState = dialogState;
            InitializeAccessors();
        }

        public void InitializeAccessors() {
            profileStateAccessor = UserState.CreateProperty<UserProfile>(UserProfileId);
            conversationDataAccessor = ConversationState.CreateProperty<ConversationData>(ConversationDataId);
            dialogStateAccessor = ConversationState.CreateProperty<DialogState>(DialogStateId);
        }
    }
}
