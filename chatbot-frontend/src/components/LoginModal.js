import React, { useState, useEffect } from 'react';
import authService from '../services/authService';
import './LoginModal.css';
import nabIcon from '../assets/nab-icon.png';

function LoginModal({ onClose, onLoginSuccess }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [demoUsers, setDemoUsers] = useState([]);

  useEffect(() => {
    loadDemoUsers();
  }, []);

  const loadDemoUsers = async () => {
    const users = await authService.getDemoUsers();
    setDemoUsers(users);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await authService.login(username, password);
      onLoginSuccess(response.token, response.username);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleDemoLogin = (demoUsername, demoPassword) => {
    setUsername(demoUsername);
    setPassword(demoPassword);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <button className="close-btn" onClick={onClose}>×</button>
        
        <div className="modal-header">
          <div className="header-title">
            <img src={nabIcon} alt="NAB" className="nab-icon" />
            <h1>NAB Internet Banking</h1>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label htmlFor="username">NAB ID</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder=""
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder=""
              required
            />
          </div>

          <div className="remember-me">
            <input
              type="checkbox"
              id="rememberMe"
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
            />
            <label htmlFor="rememberMe">Remember my NAB ID</label>
          </div>

          <p className="security-notice">
            For security reasons, we'll only show you the last 3 digits. Don't save your NAB ID if anyone else uses this browser.
          </p>

          {error && <div className="error-message">{error}</div>}

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Logging in...' : 'Login'}
          </button>

          <div className="login-links">
            <a href="#" className="forgot-link">Forgot your NAB ID or password?</a>
          </div>

          <div className="register-section">
            <span>New to NAB Internet Banking? </span>
            <a href="#" className="register-link">Register now</a>
          </div>
        </form>        
      </div>
    </div>
  );
}

export default LoginModal;
