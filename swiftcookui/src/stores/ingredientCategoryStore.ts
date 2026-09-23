import { defineStore } from 'pinia';
import api from '@/services/api';
import type { IngredientCategoryDto } from '../interfaces/ingredientCategory';
import { getErrorMessage } from '@/utils/errors';

// Read-only for now (Ticket 9) — category CRUD is deferred; see BACKLOG.md.
// Ticket 8 will consume this store to render one Advanced Search box per category.
export const useIngredientCategoryStore = defineStore('ingredientCategory', {
  state: () => ({
    categories: [] as IngredientCategoryDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get<IngredientCategoryDto[]>('/ingredientcategory');
        this.categories = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to fetch ingredient categories');
      } finally {
        this.loading = false;
      }
    },
  },
});
