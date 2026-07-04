import { defineStore } from 'pinia';
import api from '@/services/api';

export const useToolStore = defineStore('tool', {
  state: () => ({
    tools: [] as any[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
		this.loading = true
      try {
        const response = await api.get('/tool');
        this.tools = response.data;
      } catch (err) {
        console.error('Failed to fetch tools:', err);
      } finally {
		  this.loading = false
	  }
    },
	
	async createTool(tool: any) {
		try {
			const response = await api.post('/tool/post');
			this.tools.push(response.data)
			return response.data
		}
		catch (err: any)
		{
			this.error = err.message
			throw err
		}
	}
  }
});
