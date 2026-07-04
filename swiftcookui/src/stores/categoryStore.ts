import { defineStore } from 'pinia';
import api from '@/services/api';

export const useCategoryStore = defineStore('category', {
  state: () => ({
    category: null as any | null,
    categories: [] as any[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async search(query: string) {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get(`/category/search?q=${encodeURIComponent(query)}`);
        this.categories = res.data;
      } catch (err: any) {
        this.error = err.message || 'Failed to search for category';
      } finally {
        this.loading = false;
      }
    },

    async fetchAll() {
      this.loading = true;
      try {
        const res = await api.get(`/category`);
        this.categories = res.data;
      } catch (err: any) {
        this.error = err.message || 'Failed to fetch all categories';
      } finally {
        this.loading = false;
      }
    },

    async fetchActive() {
      this.loading = true;
      try {
        const res = await api.get(`/category/active`);
        this.categories = res.data;
      } catch (err: any) {
        this.error = err.message || 'Failed to fetch active categories';
      } finally {
        this.loading = false;
      }
    },

    async fetch(id: number) {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get(`/category/${id}`);
        this.category = res.data;
      } catch (err: any) {
        this.error = err.message || 'Failed to fetch category';
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
