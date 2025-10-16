import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:58550/api';

const chatService = {
  async sendMessage(message, authToken = null) {
    try {
      const headers = {};
      if (authToken) {
        headers['Authorization'] = `Bearer ${authToken}`;
      }

      const response = await axios.post(
        `${API_BASE_URL}/chat/query`,
        { message },
        { headers }
      );

      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || 'Failed to send message');
    }
  },

  async checkHealth() {
    try {
      const response = await axios.get(`${API_BASE_URL}/chat/health`);
      return response.data;
    } catch (error) {
      return null;
    }
  }
};

export default chatService;
