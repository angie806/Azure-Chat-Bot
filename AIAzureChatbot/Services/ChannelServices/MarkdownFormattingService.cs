using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAzureChatbot.Services
{
    public class ToMarkdown
    {
        /*
         * Markdown formatting helpers 
         *  Methods are named after the HTML equivalent
         */

        public static string MessageAddLi(string s) {
            return "> " + s + "\n"; 
        }

        public static string MessageWrapInStrong(string s) {
            return "*" + s + "*";
        }

        public static string MessageWrapInItalics(string s) {
            return "_" + s + "_";
        }

        public static string MessageButton(string s) {
            return "on the button below";
        }
    }
}
