import React, {Component} from 'react';

class ChatMessage extends Component{
    constructor() {
        super()
    }

    render() {
        const messageClass = this.props.isBot ? "chat__message_bot" : "chat__message_user";
        const bubbleClass = this.props.isBot ? "chat__message_botbubble" : "chat__message_userbubble";
        const receiptClass = this.props.isBot ? "chat__receipt_bot" : "chat__receipt_user";
        return(
            <div>
                <div className={"chat__message " + messageClass}>
                    <div className={"chat__message_bubble  " + bubbleClass} dangerouslySetInnerHTML={{__html: this.props.message}}>
                        
                    </div>
                </div>
                <div className={"chat__receipt " + receiptClass}>
                    <span>{this.props.isBot ? "My Chatbot" : "You"} at {this.props.timeSent}</span>
                </div>
            </div>
        );
    }
    
}

export default ChatMessage;