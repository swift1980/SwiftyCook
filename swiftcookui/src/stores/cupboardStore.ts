import { defineStore } from 'pinia';
import api from '@/services/api';
import type { CupboardDto, CupboardCreateDto } from '@/interfaces/cupboard';
import { getErrorMessage } from '@/utils/errors';

export const useCupboardStore = defineStore('cupboard', {
  state: () => ({
    items: [] as CupboardDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
      this.loading = true
      this.error = null
      try {
        const response = await api.get<CupboardDto[]>('/cupboard');
        this.items = response.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to load cupboard');
        console.error('Failed to fetch cupboard:', err);
      } finally {
        this.loading = false
      }
    },

    async addItem(dto: CupboardCreateDto) {
      this.error = null
      try {
        const res = await api.post<CupboardDto>('/cupboard', dto);
        // The server may have merged or replaced rows, so reload the list
        const rows = await api.get<CupboardDto[]>('/cupboard');
        this.items = rows.data;
        return res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to add item to cupboard');
        throw err;
      }
    },

    async removeItem(ingredientId: number, unitId: number) {
      this.error = null
      try {
        await api.delete(`/cupboard/${ingredientId}/${unitId}`);
        this.items = this.items.filter(i => !(i.ingredientId === ingredientId && i.unitId === unitId));
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to remove item from cupboard');
        throw err;
      }
    },
  }
});
