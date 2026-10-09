import { defineStore } from 'pinia';
import api from '@/services/api';
import type { CookLogDto, CookLogCreateDto, CookLogUpdateDto } from '@/interfaces/cookLog';
import { getErrorMessage } from '@/utils/errors';

export const useCookLogStore = defineStore('cookLog', {
  state: () => ({
    logs: [] as CookLogDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchForRecipe(recipeId: number) {
      this.loading = true
      this.error = null
      this.logs = []
      try {
        const res = await api.get<CookLogDto[]>('/cooklog', { params: { recipeId } });
        this.logs = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to load the cook history');
        console.error('Failed to fetch cook log:', err);
      } finally {
        this.loading = false
      }
    },

    async log(dto: CookLogCreateDto) {
      this.error = null
      try {
        await api.post('/cooklog', dto);
        await this.fetchForRecipe(dto.recipeId)
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to log the recipe');
        throw err;
      }
    },

    async update(log: CookLogDto, dto: CookLogUpdateDto) {
      this.error = null
      try {
        await api.put(`/cooklog/${log.id}`, dto);
        await this.fetchForRecipe(log.recipeId)
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to update the log entry');
        throw err;
      }
    },

    async remove(log: CookLogDto) {
      this.error = null
      try {
        await api.delete(`/cooklog/${log.id}`);
        this.logs = this.logs.filter(l => l.id !== log.id)
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to delete the log entry');
        throw err;
      }
    },
  }
});
