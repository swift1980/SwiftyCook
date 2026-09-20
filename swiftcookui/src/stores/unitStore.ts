import { defineStore } from 'pinia';
import api from '@/services/api';
import type { UnitDto, UnitCreateDto } from '@/interfaces/unit';
import { getErrorMessage } from '@/utils/errors';

export const useUnitStore = defineStore('unit', {
  state: () => ({
    units: [] as UnitDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
		this.loading = true
      try {
        const response = await api.get<UnitDto[]>('/unit');
        this.units = response.data;
      } catch (err) {
        console.error('Failed to fetch units:', err);
      } finally {
		  this.loading = false
	  }
    },
	
	async createUnit(unit: UnitCreateDto) {
		try {
			const response = await api.post<UnitDto>('/unit/post', unit);
			this.units.push(response.data)
			return response.data
		}
		catch (err: unknown)
		{
			this.error = getErrorMessage(err, 'Failed to create unit')
			throw err
		}
	}
  }
});
