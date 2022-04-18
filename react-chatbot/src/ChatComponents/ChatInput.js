import React, {Component} from 'react';
import sendchat from '../images/sendchat.svg';


class ChatInput extends Component {
    constructor() {
        super();
        this.triggerEnable = this.triggerEnable.bind(this);
    }  

    state = {
        message: '',
        disable: false,
    } 

    triggerEnable = () => {
        this.setState({disable: !this.state.disable,
        message: ''})

    }

    render() {
        return(
            <div className = "chat__input">
                <form className = "chat__input_form" action="." 
                    onSubmit = {(e) => {
                        e.preventDefault();
                        if (!this.state.disable) {
                            this.props.onSubmitMessage(this.state.message);
                            this.setState({message: ''});
                        }
                    }}
                >
                    <input type="text" className="chat__input_text" 
                                placeholder={this.state.disable ? 'Choose an option above': 'Enter message...'}
                                aria-label={this.state.disable ? 'Choose an option above': 'Enter message...'}
                                onChange={(e) => {this.setState({message: e.target.value})}}
                                value={this.state.message}
                                disabled = {this.state.disable}
                    />
                    <input className="chat__input_button" src={sendchat} width="20px" type="image"
                        // onClick={(e) => {
                        //     e.preventDefault();
                        //     if (!this.state.disable) {
                        //         this.props.onSubmitMessage(this.state.message);
                        //         this.setState({message: ''});
                        //     }
                        // }}
                    />
                </form>
            </div>
        );
    }
}

export default ChatInput;