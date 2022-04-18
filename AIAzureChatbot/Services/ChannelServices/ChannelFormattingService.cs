using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace AIAzureChatbot.Services
{
    public class ChannelFormattingService
    {
        /*
         * Function that takes in the turnContext, the message to be formatted, and the functions to map onto the message for either web chat or slack
         * 
         *  first checks if the message is to be sent to ... or Web chat and sets respective list of mappers to functionList
         *  Loops through all functions and maps it onto the message. 
         */
        public static string FormatMessages(ITurnContext turnContext, string message, List<Func<string, string>> /* [redacted] */ = null, List<Func<string, string>> /* [redacted] */ = null) {
            List<Func<string, string>> functionList;

            if (turnContext.Activity?.ChannelId == /* [redacted] */)
            {
                functionList = /* [redacted] */;
            }
            else {
                functionList = /* [redacted] */;
            }

            string value = message;
            if (functionList != null) { 
                foreach (Func<string, string> method in functionList) {
                    value = method(value);
                }
            }

            return value;
        }

        /*
         * Formats a link:
         *  Web chat: surround link in <a> tags with proper attributes
         *  ...: returns "click button below"
         */
        public static string FormatLinkMessageAndSaveToState(ITurnContext context, string url, BotStateService botService, CancellationToken cancellationToken) {
            var link = FormatMessages(context, url, new List<Func<string, string>> { ToHtml.MessageConvertIntoLink },
                    new List<Func<string, string>> { ToMarkdown.MessageButton });
            /* [redacted] */.AddURLToConversationState(botService, context, cancellationToken, url);

            return link;
        }

        /*
         * Formats a simple message containing no data, links, or buttons
         */
        public static string FormatSimpleMessage(ITurnContext turnContext, string message) {
            return FormatMessages(turnContext, message, new List<Func<string, string>> { ToHtml.MessageWrapInSpan });
        }

        /*
         * Formats a bold message 
         */
        public static string FormatBoldMessage(ITurnContext turnContext, string message) {
            return FormatMessages(turnContext, message, new List<Func<string, string>> { ToHtml.MessageWrapInStrong }, new List<Func<string, string>> { ToMarkdown.MessageWrapInStrong });
        }

        /*
         * Function takes in a header, and a dictionary of subtitles and data
         * Formats it as such for in both HTML and Mdown
         * 
         *      Bold Header (italics if ...)
         *      <hr> (if web chat)
         *      Subtitle1
         *          Data1
         *          Data2
         *      Subtitle2
         *          Data1
         *          ...
         *      
         */
        public static string FormatHeaderSubtitleAndList(ITurnContext context, string header, Dictionary<string, Dictionary<string, string>> subtitlesAndData) {
            var response = FormatMessages(context, header,
                new List<Func<string, string>> { ToHtml.MessageWrapInStrong, ToHtml.MessageWrapInSpan, ToHtml.MessageAddHr },
                new List<Func<string, string>> { ToMarkdown.MessageWrapInItalics, ToMarkdown.MessageWrapInStrong }) + "\n";

            foreach ((string subtitle, Dictionary<string, string> attributes) in subtitlesAndData)
            {
                response += FormatMessages(context, $"{subtitle}:", new List<Func<string, string>> { ToHtml.MessageWrapInStrong, ToHtml.MessageWrapInSpan },
                    new List<Func<string, string>> { ToMarkdown.MessageWrapInStrong }) + "\n";

                foreach ((string attr, string value) in attributes)
                {
                    var boldAttr = FormatBoldMessage(context, attr);
                    response += FormatMessages(context, $"{boldAttr}: {value}", new List<Func<string, string>> { ToHtml.MessageWrapInLi, ToHtml.MessageWrapInUl },
                        new List<Func<string, string>> { ToMarkdown.MessageAddLi });
                }
            }

            return response;
        }


        /*
         * Function takes in a header, and a dictionary of subtitles and data
         * Formats it as such for in both HTML and Mdown
         * 
         *
         *      bold Subtitle1
         *      <hr> if webchat
         *          Data1
         *          Data2
         *      bold Subtitle2
         *      <hr>if webchat
         *          Data1
         *          ...
         *      
         */
        public static string FormatSubtitleAndList(ITurnContext context, Dictionary<string, Dictionary<string, string>> subtitlesAndData) {
            var message = "";

            foreach ((string ticker, Dictionary<string, string> attributes) in subtitlesAndData)
            {
                if (attributes.Count > 0)
                {
                    message += FormatMessages(context, $"{ticker} fund attributes:",
                        new List<Func<string, string>> { ToHtml.MessageWrapInStrong, ToHtml.MessageWrapInSpan, ToHtml.MessageAddHr },
                        new List<Func<string, string>> { ToMarkdown.MessageWrapInItalics, ToMarkdown.MessageWrapInStrong }) + "\n";

                    var attrMsg = "";
                    foreach ((string attr, string value) in attributes)
                    {
                        var boldAttr = FormatBoldMessage(context, attr);
                        attrMsg += FormatMessages(context, $"{boldAttr}: {value}",
                            new List<Func<string, string>> { ToHtml.MessageWrapInLi }, new List<Func<string, string>> { ToMarkdown.MessageAddLi });
                    }

                    message += FormatMessages(context, attrMsg, new List<Func<string, string>> { ToHtml.MessageWrapInUl });
                }
            }

            return message;
        }
    }
}
