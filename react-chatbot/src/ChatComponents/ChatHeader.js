import React, {Component} from 'react';
import minus from '../images/minus.svg';
import close from '../images/close.svg';

class ChatHeader extends Component {

    constructor() {
        super();
        this.changeOnlineStatus = this.changeOnlineStatus.bind(this);
    }

    state = {
        isOnline: false,
    }

    changeOnlineStatus = () => {
        this.setState({
            isOnline: !this.state.isOnline
        });
    }

    render() {
        return(
            <div className="chat__header">
                <div className="chat__header_images">
                    <div className="chat__header_img">
                        <input type="image" className="chat__minimize" src={minus} width='20px' onClick={(e) => {this.props.minimizeChat()}}/>
                    </div>
                    <div className="chat__header_img">
                        <input type="image" className="chat__close" src={close} width='20px' onClick={(e) => {this.props.closeChat()}}/>
                    </div>
                </div>
                <div className="chat__header_title">
                    <h4>My Chatbot</h4>
                </div>
                <div className="chat__header_onlineIndicator">
                    <div className="chat__header_indicator"
                        style={{backgroundColor: this.state.isOnline ? 'green': 'lightgray'}}
                    >

                    </div>
                </div>
            </div>
        );
    }
}

export default ChatHeader;