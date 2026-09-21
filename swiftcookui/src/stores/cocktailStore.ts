import { defineStore } from 'pinia';
import api from '@/services/api';
import type { RecipeDto } from "../interfaces/recipe";
import { getErrorMessage } from '@/utils/errors';

export const useCocktailStore = defineStore('cocktail', {
  state: () => ({
    cocktail: null as RecipeDto | null,
    cocktails: [] as RecipeDto[],
    error: null as string | null,
    loading: false,
  }),

  getters: {
    getFilteredCocktails: (state) => (nameQ: string) => {
      if (!nameQ) return state.cocktails
      return state.cocktails.filter(cocktail =>
        cocktail.name.toLowerCase().includes(nameQ.toLowerCase())
      )
    }
  },

  actions: {
    async searchCocktails(query: string) {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get<RecipeDto[]>(`/cocktail/search?q=${encodeURIComponent(query)}`);
        this.cocktails = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to search for cocktail');
      } finally {
        this.loading = false;
      }
    },

    async fetchCocktailAll() {
      this.loading = true;
      try {
        const res = await api.get<RecipeDto[]>('/cocktail');
        this.cocktails = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to fetch all cocktails');
      } finally {
        this.loading = false;
      }
    },

    async fetchCocktail(id: number) {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get<RecipeDto>(`/cocktail/${id}`);
        this.cocktail = res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to fetch cocktail');
      } finally {
        this.loading = false;
      }
    },

    reset() {
      this.cocktail = null;
      this.cocktails = [];
      this.error = null;
      this.loading = false;
    },
  },
});
