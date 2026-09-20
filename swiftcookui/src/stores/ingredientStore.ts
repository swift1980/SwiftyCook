import { defineStore } from 'pinia';
import api from '@/services/api';
import type { IngredientDto } from "../interfaces/ingredient";
import type { IngredientCreateDto } from "../interfaces/ingredient";
import type { IngredientTypeDto } from "../interfaces/ingredientType";
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

    getIngredientsGroupedByTypeWithNames: (state): Array<{ type: IngredientTypeDto; ingredients: IngredientDto[] }> => {
      const groups = new Map<number, { type: IngredientTypeDto; ingredients: IngredientDto[] }>();
      for (const ingredient of state.ingredients) {
        if (ingredient.typeId == null) continue; // skip untyped ingredients
        const existing = groups.get(ingredient.typeId);
        if (existing) {
          existing.ingredients.push(ingredient);
        } else {
          groups.set(ingredient.typeId, {
            type: { id: ingredient.typeId, name: ingredient.typeName ?? '' },
            ingredients: [ingredient],
          });
        }
      }
      return Array.from(groups.values());
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
