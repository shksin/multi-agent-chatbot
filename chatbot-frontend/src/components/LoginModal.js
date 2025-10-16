import React, { useState, useEffect } from 'react';
import authService from '../services/authService';
import './LoginModal.css';

function LoginModal({ onClose, onLoginSuccess }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
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
        <div className="modal-header">
          <h2>Login to Your Account</h2>
          <button className="close-btn" onClick={onClose}>×</button>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter username"
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
              placeholder="Enter password"
              required
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Logging in...' : 'Login'}
          </button>
        </form>

        {demoUsers.length > 0 && (
          <div className="demo-users">
            <div className="divider">
              <span>Demo Accounts</span>
            </div>
            <p className="demo-hint">Click to auto-fill credentials:</p>
            <div className="demo-users-list">
              {demoUsers.map((user) => (
                <div
                  key={user.username}
                  className="demo-user-card"
                  onClick={() => handleDemoLogin(user.username, user.password)}
                >
                  <div className="demo-user-header">
                    <span className="demo-user-icon">👤</span>
                    <strong>{user.username}</strong>
                  </div>
                  <p className="demo-user-desc">{user.description}</p>
                  <div className="demo-credentials">
                    <code>User: {user.username}</code>
                    <code>Pass: {user.password}</code>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default LoginModal;
