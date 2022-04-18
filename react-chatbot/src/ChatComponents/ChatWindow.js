import React, {Component} from 'react';

import bubble from '../images/bubble.svg';

import ChatMessage from './ChatMessage';
import ChatInput from './ChatInput';
import ChatHeader from './ChatHeader';
import ChatSuggestedActions from './ChatSuggestedActions';

import GetDLBearerToken from './ChatConnectivityHelpers';

var https = require('https');

class ChatWindow extends Component {
    constructor() {
        super();
        this.minimizeChat = this.minimizeChat.bind(this);
        this.closeChat = this.closeChat.bind(this);

        this.startWebSocketStream = this.startWebSocketStream.bind(this);
        this.sendConversationUpdateActivity = this.sendConversationUpdateActivity.bind(this);
        this.sendActivity = this.sendActivity.bind(this);
        this.parseMessages = this.parseMessages.bind(this);
        this.sendSuggestedActionActivity = this.sendSuggestedActionActivity.bind(this);
        this.sendMessageActivity = this.sendMessageActivity.bind(this);

        this.chatInputElement = React.createRef();
        this.chatHeaderElement = React.createRef();
        this.chatSAelement = React.createRef();
    }

    state = {
        /* [redacted] */
    }

    ws;

    componentDidMount() {
        let self = this;

        GetDLBearerToken().then(function(response) {
            console.log("response");
            return response.json();
        }).then(function(json){
            console.log(json);
            self.setState({
               /* [redacted] */
            });
            console.log(/* [redacted] */);
            self.chatHeaderElement.current.changeOnlineStatus();
            self.startWebSocketStream();
        }); 
    }

    startWebSocketStream = () => {
        this.ws = new WebSocket(this.state.streamUrl);

        this.ws.onopen = () => {
            console.log('connected')
        }

        this.ws.onmessage = evt => {
            if (evt.data !== "")
                this.parseMessages(JSON.parse(evt.data));
                console.log(evt.data);
        }

        this.ws.onclose = () => {
            console.log('disconnected')
            this.setState({
                ws: new WebSocket(URL),
            })
        }
        this.sendConversationUpdateActivity();
    }

    parseMessages = (data) => {
        if (data["activities"] != null) {
            /* [redacted] */
            
        }    
    }

    scrollMessageWindowIntoView = () => {
        var elem = document.getElementById("chat_message_window");
        elem.scrollTop = elem.scrollHeight;    
    }

    sendConversationUpdateActivity = () => {
        var info = JSON.stringify({
            /* [redacted] */
        })
        this.sendActivity(info);
    }

    sendSuggestedActionActivity = (message) => {
        this.setState({
            /* [redacted] */
        });
        this.chatSAelement.current.addActions([]);
        this.chatInputElement.current.triggerEnable();
        this.sendMessageActivity(message);
    }

    sendMessageActivity = (message) => {
        if (message.trim() !== "") {
            const formatMessage = "<span>" + message + "</span>"

            this.setState({
                /* [redacted] */
            });

            var info = JSON.stringify({
                /* [redacted] */
            });

            this.scrollMessageWindowIntoView();
            this.sendActivity(info);
        }
    }  

    sendActivity = (info) => {
        var options = {
           /* [redacted] */                                
        };
        
        var request = https.request(options, (res) => {
            var body = [];
            res.on('data', (d) => {
                body.push(d);
            });
            
            res.on('end', () => {
                var result = JSON.parse(Buffer.concat(body).toString());
            });
        });
        
        request.write(info);
        request.end();
        request.on('error', (err) => {
            console.log(err);
        });   
    }

    minimizeChat = () => {
        this.setState({
            isMinimized: !this.state.isMinimized
        });
    }

    closeChat = () => {
        this.setState({
            isClosed: !this.state.isClosed
        });
    }

    render() {
        let saElement;
        if (this.state.hasSuggestedActions) {
            saElement = <ChatSuggestedActions ref={this.chatSAelement} onActionClicked = {this.sendSuggestedActionActivity} />
        } else {
            saElement = '';
        }

        return(
        <div>
            <div className="chat__minbubble" onClick={(e) => {this.minimizeChat()}} style={{display: (!this.state.isMinimized || this.state.isClosed) ? 'none' : 'flex'}}>
                <img src={bubble} width="40px" />
            </div>

            <div className="chat__mainWindow"  style={{display: (this.state.isMinimized || this.state.isClosed) ? 'none' : 'flex'}}>
                <ChatHeader 
                    minimizeChat={this.minimizeChat}
                    closeChat={this.closeChat}
                    ref={this.chatHeaderElement}
                />

                <div className="chat__messageWindow" id="chat_message_window"
                    style={ 
                        this.state.hasSuggestedActions ? 
                            {
                                maxHeight: '355px',
                                height: '355px',
                                minHeight: '355px',
                                bottom: '115px',
                                
                            } :
                            {
                                maxHeight: '420px',
                                height: '420px',
                                minHeight: '420px',
                                bottom: '50px',
                            }
                    }
                >
                    {this.state.messages.map((message, index) =>
                        <ChatMessage
                            key={index}
                            message={message.message}
                            isBot={message.isBot}
                            timeSent={message.timeSent}
                        />,
                    )}
                </div>

                <div className="chat__suggestaction"
                    style={ 
                        this.state.hasSuggestedActions ? 
                            {
                                maxHeight: '65px',
                                height: '65px',
                                minHeight: '65px',
                                bottom: '50px',
                            } :
                            {
                                display: 'none'
                            }
                    }
                >
                    {saElement}
                </div>

                <ChatInput ref={this.chatInputElement} onSubmitMessage={this.sendMessageActivity}/>
            </div>
        </div>
        )
    }
}

export default ChatWindow;