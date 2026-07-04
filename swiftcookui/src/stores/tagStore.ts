import { defineStore } from 'pinia';
import api from '@/services/api';

export const useTagStore = defineStore('tag', {
  state: () => ({
    tags: [] as any[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
		this.loading = true
      try {
        const response = await api.get('/tag');
        this.tags = response.data;
      } catch (err) {
        console.error('Failed to fetch tags:', err);
      } finally {
		  this.loading = false
	  }
    },
	
	async createTag(tag: any) {
		try {
			const response = await api.post('/tag/post');
			this.tags.push(response.data)
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
