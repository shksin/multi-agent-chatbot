import React, { useState, useEffect } from 'react';
import './App.css';
import ChatInterface from './components/ChatInterface';
import LoginModal from './components/LoginModal';
import authService from './services/authService';
import nabLogo from './assets/nab-logo.svg';
import nabIcon from './assets/nab-icon.png';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [user, setUser] = useState(null);
  const [authToken, setAuthToken] = useState(null);
  const [showChatbot, setShowChatbot] = useState(false);

  useEffect(() => {
    // Check if user is already logged in (token in localStorage)
    const token = localStorage.getItem('authToken');
    const userData = localStorage.getItem('user');
    
    if (token && userData) {
      setAuthToken(token);
      setUser(JSON.parse(userData));
      setIsLoggedIn(true);
    }
  }, []);

  const handleLoginSuccess = (token, username) => {
    setAuthToken(token);
    setUser({ username });
    setIsLoggedIn(true);
    setShowLoginModal(false);
    
    // Store in localStorage
    localStorage.setItem('authToken', token);
    localStorage.setItem('user', JSON.stringify({ username }));
  };

  const handleLogout = () => {
    setAuthToken(null);
    setUser(null);
    setIsLoggedIn(false);
    
    // Clear localStorage
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');
  };

  return (
    <div className="App">
      {/* Navigation Header */}
      <header className="nav-header">
        <div className="nav-content">
          <div className="logo">
            <img src={nabLogo} alt="NAB" className="logo-img" />
          </div>
          <nav className="nav-menu">
            <a href="#" className="nav-item active">Personal</a>
            <a href="#" className="nav-item">Business</a>
            <a href="#" className="nav-item">Corporate</a>
            <a href="#" className="nav-item">About us</a>
            <a href="#" className="nav-item">Help and support</a>
          </nav>
          <div className="nav-actions">
            <button className="internet-banking-btn">Internet Banking</button>
            {isLoggedIn ? (
              <div className="user-info">
                <span className="user-name">{user?.username}</span>
                <button className="btn btn-logout" onClick={handleLogout}>
                  Logout
                </button>
              </div>
            ) : (
              <button className="login-btn" onClick={() => setShowLoginModal(true)}>
                Login
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="hero-section">
        <div className="hero-content">
          <div className="hero-card">
            <h1>2025 Cyber Month</h1>
            <p>Building a cyber safe culture</p>
            <button className="explore-btn">Explore Cyber Month</button>
          </div>
        </div>
      </section>

      {/* Banking Solutions */}
      <section className="banking-solutions">
        <div className="solutions-container">
          <h2>Popular banking solutions</h2>
          <p>We're here to make banking simpler and easier with our popular products, special offers and helpful calculators.</p>
          
          <div className="solutions-grid">
            <div className="solution-category">
              <div className="solution-icon">🏠</div>
              <h3>Home loans</h3>
              <ul>
                <li><a href="#">What will my repayments be?</a></li>
                <li><a href="#">How much can I borrow?</a></li>
                <li><a href="#">Talk to an expert</a></li>
              </ul>
            </div>
            
            <div className="solution-category">
              <div className="solution-icon">💳</div>
              <h3>Credit cards</h3>
              <ul>
                <li><a href="#">Help me choose a credit card</a></li>
                <li><a href="#">Compare credit cards</a></li>
                <li><a href="#">Credit card balance transfers</a></li>
              </ul>
            </div>
            
            <div className="solution-category">
              <div className="solution-icon">🏦</div>
              <h3>Bank accounts</h3>
              <ul>
                <li><a href="#">Transaction accounts</a></li>
                <li><a href="#">Savings accounts</a></li>
                <li><a href="#">Smart Pay Later</a></li>
              </ul>
            </div>
            
            <div className="solution-category">
              <div className="solution-icon">💰</div>
              <h3>Personal loans</h3>
              <ul>
                <li><a href="#">Borrowing power calculator</a></li>
                <li><a href="#">Loan repayment calculator</a></li>
                <li><a href="#">Debt consolidation calculator</a></li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      {/* Chatbot Widget */}
      <div className="chatbot-widget">
        {showChatbot ? (
          <div className="chatbot-container">
            <div className="chatbot-header">
              <div className="chatbot-info">
                <div className="chatbot-avatar">
                  <img src={nabIcon} alt="NAB" className="avatar-img" />
                </div>
                <div>
                  <div className="chatbot-name">NAB</div>
                  <div className="chatbot-status">Message us 24/7</div>
                </div>
              </div>
              <button 
                className="chatbot-close" 
                onClick={() => setShowChatbot(false)}
              >
                ×
              </button>
            </div>
            <div className="chatbot-content">
              <ChatInterface authToken={authToken} isLoggedIn={isLoggedIn} />
            </div>
          </div>
        ) : (
          <button 
            className="chatbot-trigger" 
            onClick={() => setShowChatbot(true)}
          >
            <div className="chatbot-trigger-avatar">
              <img src={nabIcon} alt="NAB" className="avatar-img" />
            </div>
            <div className="chatbot-trigger-text">
              <div className="chatbot-trigger-name">NAB Virtual Assistant</div>
              <div className="chatbot-trigger-message">Hi, I'm here to help if you have any questions.</div>
            </div>
            <div className="chatbot-trigger-icon">
              <div className="chat-bubble">
                <div className="chat-dots">•••</div>
              </div>
            </div>
          </button>
        )}
      </div>

      {showLoginModal && (
        <LoginModal
          onClose={() => setShowLoginModal(false)}
          onLoginSuccess={handleLoginSuccess}
        />
      )}
    </div>
  );
}

export default App;
