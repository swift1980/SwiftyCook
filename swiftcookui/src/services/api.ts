import axios from 'axios';

const api = axios.create({
  baseURL: '/api', // matches your ASP.NET API
  headers: {
    'Content-Type': 'application/json'
  }
});

export default api;
