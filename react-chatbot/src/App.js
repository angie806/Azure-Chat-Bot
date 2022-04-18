import React from 'react';
import './stylesheets/App.css';
import './stylesheets/Chat.css'
import ChatWindow from './ChatComponents/ChatWindow'

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <ChatWindow />
      </header>
    </div>
  );
}

export default App;
