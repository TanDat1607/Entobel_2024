import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:31195/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// check stored token
const token = localStorage.getItem('accessToken');
if (token) {
  api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
}

export default api;