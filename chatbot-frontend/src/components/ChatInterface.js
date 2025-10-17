import React, { useState, useRef, useEffect } from 'react';
import ReactMarkdown from 'react-markdown';
import chatService from '../services/chatService';
import './ChatInterface.css';
import nabIcon from '../assets/nab-red-icon.jpg';

function ChatInterface({ authToken, isLoggedIn }) {
  const [messages, setMessages] = useState([
    {
      id: 1,
      type: 'bot',
      text: "👋 Hello! I'm your multi-agent assistant. I can help you with information from our knowledge base" + 
            (isLoggedIn ? " and your personal account data." : ". Login to access personalized information!"),
      timestamp: new Date(),
      agents: []
    }
  ]);
  const [inputMessage, setInputMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  useEffect(() => {
    // Update welcome message when login status changes
    setMessages(prev => [{
      id: 1,
      type: 'bot',
      text: "👋 Hello! I'm your multi-agent assistant. I can help you with information from our knowledge base" + 
            (isLoggedIn ? " and your personal account data." : ". Login to access personalized information!"),
      timestamp: new Date(),
      agents: []
    }]);
  }, [isLoggedIn]);

  const handleSendMessage = async (e) => {
    e.preventDefault();
    
    if (!inputMessage.trim() || isLoading) {
      return;
    }

    const userMessage = {
      id: Date.now(),
      type: 'user',
      text: inputMessage,
      timestamp: new Date()
    };

    setMessages(prev => [...prev, userMessage]);
    setInputMessage('');
    setIsLoading(true);

    try {
      const response = await chatService.sendMessage(inputMessage, authToken);
      
      const botMessage = {
        id: Date.now() + 1,
        type: 'bot',
        text: response.message,
        timestamp: new Date(response.timestamp),
        agents: response.agentsCalled || [],
        hasUserContext: response.hasUserContext,
        success: response.success
      };

      setMessages(prev => [...prev, botMessage]);
    } catch (error) {
      const errorMessage = {
        id: Date.now() + 1,
        type: 'bot',
        text: `❌ Error: ${error.message}. Please try again.`,
        timestamp: new Date(),
        agents: [],
        success: false
      };

      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const suggestedQuestions = [
    "Tell me about your products",
    "What support options are available?",
    "Show me my account information",
    "What's my recent activity?",
    "Display my usage statistics"
  ];

  const handleSuggestedQuestion = (question) => {
    setInputMessage(question);
  };

  return (
    <div className="chat-interface">
      <div className="chat-container">
        <div className="messages-container">
          {messages.map((message) => (
            <div key={message.id} className={`message ${message.type}`}>
              <div className="message-content">
                <div className="message-header">
                  <span className="message-icon">
                    {message.type === 'user' ? '👤' : <img src={nabIcon} alt="NAB" className="message-icon-img" />}
                  </span>
                  <span className="message-time">
                    {message.timestamp.toLocaleTimeString()}
                  </span>
                </div>
                <div className="message-text">
                  <ReactMarkdown>{message.text}</ReactMarkdown>
                </div>
                {message.agents && message.agents.length > 0 && (
                  <div className="message-agents">
                    <span className="agents-label">Agents:</span>
                    {message.agents.map((agent, idx) => (
                      <span key={idx} className="agent-badge">
                        {agent}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ))}
          
          {isLoading && (
            <div className="message bot">
              <div className="message-content">
                <div className="message-header">
                  <span className="message-icon">
                    <img src={nabIcon} alt="NAB" className="message-icon-img" />
                  </span>
                  <span className="message-time">Processing...</span>
                </div>
                <div className="typing-indicator">
                  <span></span>
                  <span></span>
                  <span></span>
                </div>
              </div>
            </div>
          )}
          
          <div ref={messagesEndRef} />
        </div>

        {messages.length === 1 && (
          <div className="suggested-questions">
            <p className="suggestions-title">Try asking:</p>
            <div className="suggestions-list">
              {suggestedQuestions.map((question, idx) => (
                <button
                  key={idx}
                  className="suggestion-btn"
                  onClick={() => handleSuggestedQuestion(question)}
                  disabled={isLoading}
                >
                  {question}
                </button>
              ))}
            </div>
          </div>
        )}

        <form onSubmit={handleSendMessage} className="input-container">
          <input
            type="text"
            value={inputMessage}
            onChange={(e) => setInputMessage(e.target.value)}
            placeholder="Type your message here..."
            disabled={isLoading}
            className="message-input"
          />
          <button
            type="submit"
            disabled={isLoading || !inputMessage.trim()}
            className="send-button"
          >
            {isLoading ? '⏳' : 'Send'}
          </button>
        </form>
      </div>
    </div>
  );
}

export default ChatInterface;
