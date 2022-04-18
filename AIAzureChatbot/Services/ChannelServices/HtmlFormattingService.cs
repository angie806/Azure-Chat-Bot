using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.Services
{
    public class ToHtml
    {
        /*
         * HTML Formatting Helpers
         */

        public static string MessageWrapInSpan(string s) {
            return "<span>" + s + "</span>";
        }

        public static string MessageWrapInLi(string s) {
            return "<li>" + s + "</li>\n";
        }

        public static string MessageWrapInStrong(string s)
        {
            return "<strong>" + s + "</strong>";
        }

        public static string MessageWrapInUl(string s) {
            return "<ul>" + s + "</ul>";
        }

        public static string MessageAddHr(string s) {
            return s + "<hr>";
        }

        public static string MessageAddUl(string s, bool isPrepend)
        {
            return isPrepend ? s + "\n<ul>\n" : s + "\n</ul>\n";
        }

        public static string MessageConvertIntoLink(string url) {
            return $"<a href='{url}' target='blank'>here</a>";
        }

        
    }
}
