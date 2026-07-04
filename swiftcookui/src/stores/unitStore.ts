import { defineStore } from 'pinia';
import api from '@/services/api';

export const useUnitStore = defineStore('unit', {
  state: () => ({
    units: [] as any[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
		this.loading = true
      try {
        const response = await api.get('/unit');
        this.units = response.data;
      } catch (err) {
        console.error('Failed to fetch units:', err);
      } finally {
		  this.loading = false
	  }
    },
	
	async createUnit(unit: any) {
		try {
			const response = await api.post('/unit/post');
			this.units.push(response.data)
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
