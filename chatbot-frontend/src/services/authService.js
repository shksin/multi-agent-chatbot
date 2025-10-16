import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:58550/api';

const authService = {
  async login(username, password) {
    try {
      const response = await axios.post(`${API_BASE_URL}/auth/login`, {
        username,
        password
      });
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || 'Login failed');
    }
  },

  async validateToken(token) {
    try {
      const response = await axios.post(`${API_BASE_URL}/auth/validate`, {
        token
      });
      return response.data.isValid;
    } catch (error) {
      return false;
    }
  },

  async getDemoUsers() {
    try {
      const response = await axios.get(`${API_BASE_URL}/auth/demo-users`);
      return response.data.users;
    } catch (error) {
      return [];
    }
  }
};

export default authService;
