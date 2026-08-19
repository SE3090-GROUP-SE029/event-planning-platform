import apiClient from "../../../shared/api/apiClient";

export const testApi = {
    ping: async () => {
        const response = await apiClient.get('api/test/ping');
        return response.data;
    },

    createMessage: async (message) => {
        const response = await apiClient.post('/api/test/message', {message});
        return response.data
    },

    getMessage: async (id) => {
        const response = await apiClient.get(`/api/test/message/${id}`);
        return response.data;
    },
};