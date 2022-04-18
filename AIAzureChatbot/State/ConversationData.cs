using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.State
{
    public class ConversationData
    {
        public bool HasUserName { get; set; } = false;
        public bool HasUserType { get; set; } = false;

        public bool DidCancel { get; set; } = false;
        public bool IsCancellable { get; set; } = true;

        public bool FailedGetUserName { get; set; } = false;

        public int FailedAttemptsCount { get; set; } = 0;

        public string /* [redacted] */ { get; set; } = null;
    }
}
