using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Configuration;

namespace AIAzureChatbot.Services
{
    public class RecognitionServices
    {

        //Instantiates the LUISRecognizer and QnaMaker object by passing the configuration keys. 
        public RecognitionServices(IConfiguration configuration) {
            Dispatch = new LuisRecognizer(
                    /* [redacted] */
                );

            QnaRecognizer = new QnAMaker(new QnAMakerEndpoint
            {
                /* [redacted] */
            });
        }

        public LuisRecognizer Dispatch { get; private set; }
        public QnAMaker QnaRecognizer { get; private set; }
    }
}
