import { defineStore } from 'pinia';
import api from '@/services/api';
import type { IngredientTypeDto } from '../interfaces/ingredientType';
import type { IngredientTypeCreateDto } from '../interfaces/ingredientType';
import type { IngredientDto } from '../interfaces/ingredient';

export const useIngredientTypeStore = defineStore('ingredientType', {
  state: () => ({
    ingredientTypes: [] as IngredientTypeDto[],
    typeIngredients: [] as IngredientDto[],
    error: null as string | null,
    loading: false,
  }),

  getters: {
    getIngredientsByTypeId: (state) => {
      return (typeId: number): IngredientDto[] =>
        state.typeIngredients.filter((i) => i.typeId === typeId);
    },
  },

  actions: {
    async fetchAll() {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get<IngredientTypeDto[]>('/ingredienttype');
        this.ingredientTypes = res.data;
      } catch (err: any) {
        this.error = err.message || 'Failed to fetch ingredient types';
      } finally {
        this.loading = false;
      }
    },

    async fetchIngredientsByType(typeId: number) {
      this.loading = true;
      this.error = null;
      try {
        const res = await api.get<IngredientDto[]>(`/ingredienttype/${typeId}/ingredients`);
        this.typeIngredients = res.data;
      } catch (err: any) {
        this.error = err.message || 'Failed to fetch ingredients for the type';
        this.typeIngredients = [];
      } finally {
        this.loading = false;
      }
    },

    clearTypeIngredients() {
      this.typeIngredients = [];
    },

    async createIngredientType(dto: IngredientTypeCreateDto) {
      this.error = null;
      try {
        const res = await api.post<IngredientTypeDto>('/ingredienttype', dto);
        this.ingredientTypes.push(res.data);
        return res.data;
      } catch (err: any) {
        this.error = err.message || 'Failed to create ingredient type';
        throw err;
      }
    },
  },
});
