import React, {Component} from 'react';
import {Button, ButtonToolbar} from 'react-bootstrap';
import leftArrow from '../images/leftArrow.svg';
import rightArrow from '../images/rightArrow.svg';

class ChatSuggestedActions extends Component {

    constructor(props) {
        super(props);
        this.addActions = this.addActions.bind(this);
        this.scrollLeft = this.scrollLeft.bind(this);
        this.scrollRight = this.scrollRight.bind(this);
    }

    state = {
        actions: [],
        isScrolling: false,
        scrollIndex: 0
    }

    addActions = (act) => {
        this.setState({
            actions: act
        });

        const elem = document.getElementById("suggestActionButtonToolbar");
        console.log("scroll " + elem.scrollWidth);
        console.log("client " +elem.clientWidth);
        if (elem.scrollWidth > elem.clientWidth + 10) {
            this.setState({isScrolling: true})
        }
    }

    scrollLeft = () => {
        if (this.state.scrollIndex - 1 >= 0) {
            var toolbarLeft = document.getElementById("suggestActionButtonToolbar").offsetLeft;
            var classString = "chat_button_"+(this.state.scrollIndex-1);
            var elementLeft = document.getElementById(classString).offsetLeft;            
            var scrollPosition = elementLeft - toolbarLeft;

            this.setState({scrollIndex: this.state.scrollIndex - 1});

            this.scrollAnimate(scrollPosition, -1)
        }
    }

    scrollRight = () => {
        if (this.state.scrollIndex + 1 < this.state.actions.length) {
            var toolbarLeft = document.getElementById("suggestActionButtonToolbar").offsetLeft;
            var classString = "chat_button_"+(this.state.scrollIndex+1);
            var elementLeft = document.getElementById(classString).offsetLeft;
            var scrollPosition = elementLeft - toolbarLeft;

            this.setState({scrollIndex: this.state.scrollIndex + 1});

            this.scrollAnimate(scrollPosition, 1)
            
        }
    }

    scrollAnimate = (scrollPosition, increment) => {
        var toolbar = document.getElementById("suggestActionButtonToolbar");
        var curr = toolbar.scrollLeft;
        var prev = toolbar.scrollLeft;
        var id = setInterval(frame, 5);
        function frame() {
            if (curr == scrollPosition || prev != curr ) {
                clearInterval(id);
            } else {
                curr = curr + increment;
                toolbar.scrollLeft = curr;
                prev = toolbar.scrollLeft;
            }
        }
    }

    componentDidMount() {
        console.log("update");
          
    }

    render() {
        let self = this;
        return(
            <div>
                <div className="chat__button_scroll" style={
                    this.state.isScrolling ?
                    {
                        width: '6%',
                        display: 'block'
                    } : 
                    {
                        width: '0%',
                        display: 'none'
                    }
                }>
                    <div className="chat__button_scrollImage">
                        <input src={leftArrow} width="20px" type="image"
                            onClick = {(e) => {
                                e.preventDefault();
                                this.scrollLeft();
                            }}
                        />
                    </div>
                </div>
                <ButtonToolbar id="suggestActionButtonToolbar" style={
                    this.state.isScrolling ?
                    {
                        width: '88%'
                    } : 
                    {
                        width: '100%'
                    }
                }>
                    {this.state.actions.map(function(act, index) {
                        
                        return (
                        <div className="chat__button" key={"div_"+index} id={"chat_button_" + index} >
                            <Button key={index} style={{height: '40px'}} value={act} 
                            variant="outline-primary"
                            onClick={(e) => {
                                e.preventDefault();
                                self.props.onActionClicked(act);
                            }}
                            >{act}</Button>
                        </div>)
                    })}
                </ButtonToolbar>
                <div className="chat__button_scroll" style={
                    this.state.isScrolling ?
                    {
                        width: '6%',
                        display: 'block'
                    } : 
                    {
                        width: '0%',
                        display: 'none'
                    }
                }>

                    <div className="chat__button_scrollImage">
                        <input src={rightArrow} width="20px" type="image"
                            onClick = {(e) => {
                                e.preventDefault();
                                this.scrollRight();
                            }}
                        />
                    </div>
                </div>
            </div>
        );
    }
}

export default ChatSuggestedActions;