import axios from 'axios';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5207';

const apiClient = axios.create({
    baseURL, 
    headers: {
        'Content-Type': 'application/json'
    },
});

apiClient.interceptors.request.use((config) => {
    console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`);
    return config;
});

apiClient.interceptors.response.use(
    (response) => {
        console.log(`[API] Response: ${response.status}`, response.data);
        return response;
    },

    (error) => {
        console.error(`[API] Error: ${error.response?.status}`, error.response?.data || error.message);
        return Promise.reject(error);
    }
);

export default apiClient;