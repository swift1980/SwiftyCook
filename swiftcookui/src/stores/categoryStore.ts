import { defineStore } from 'pinia';
import api from '@/services/api';
import type { CategoryDto } from '@/interfaces/category';
import { getErrorMessage } from '@/utils/errors';

export const useCategoryStore = defineStore('category', {
  state: () => ({
    category: null as CategoryDto | null,
    categories: [] as CategoryDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async search(query: string) {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get<CategoryDto[]>(`/category/search?q=${encodeURIComponent(query)}`);
        this.categories = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to search for category');
      } finally {
        this.loading = false;
      }
    },

    async fetchAll() {
      this.loading = true;
      try {
        const res = await api.get<CategoryDto[]>(`/category`);
        this.categories = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to fetch all categories');
      } finally {
        this.loading = false;
      }
    },

    async fetchActive() {
      this.loading = true;
      try {
        const res = await api.get<CategoryDto[]>(`/category/active`);
        this.categories = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to fetch active categories');
      } finally {
        this.loading = false;
      }
    },

    async fetch(id: number) {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get<CategoryDto>(`/category/${id}`);
        this.category = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to fetch category');
      } finally {
        this.loading = false;
      }
    },

    reset() {
      this.category = null;
      this.categories = [];
      this.error = null;
      this.loading = false;
    },
  },
});
