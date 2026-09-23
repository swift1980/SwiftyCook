import { defineStore } from 'pinia';
import api from '@/services/api';
import type { IngredientDto } from "../interfaces/ingredient";
import type { IngredientCreateDto } from "../interfaces/ingredient";
import { getErrorMessage } from '@/utils/errors';

export const useIngredientStore = defineStore('ingredient', {
  state: () => ({
    ingredients: [] as IngredientDto[],
    error: null as string | null,
  }),

  getters: {
    getIngredientsByTypeId: (state) => {
      return (typeId: number): IngredientDto[] =>
        state.ingredients.filter((i) => i.typeId === typeId);
    },

    getIngredientsGroupedByType: (state): Map<number, IngredientDto[]> => {
      const groups = new Map<number, IngredientDto[]>();
      for (const ingredient of state.ingredients) {
        if (ingredient.typeId == null) continue; // skip untyped ingredients
        const bucket = groups.get(ingredient.typeId);
        if (bucket) {
          bucket.push(ingredient);
        } else {
          groups.set(ingredient.typeId, [ingredient]);
        }
      }
      return groups;
    },
  },

  actions: {
    async fetchAll() {
      try {
        const response = await api.get<IngredientDto[]>('/ingredient');
        this.ingredients = response.data;
      } catch (err) {
        console.error('Failed to fetch ingredients:', err);
      }
    },
    async createIngredient(dto: IngredientCreateDto) {
      this.error = null;
      try {
        const res = await api.post<IngredientDto>('/ingredient', dto);
        this.ingredients.push(res.data); // add to local list
        return res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to create ingredient');
        throw err;
      }
    },
  }
});
