import { defineStore } from 'pinia';
import api from '@/services/api';
import type { ShoppingListDto, ShoppingListCreateDto } from '@/interfaces/shoppingList';
import { getErrorMessage } from '@/utils/errors';

export const useShoppingListStore = defineStore('shoppingList', {
  state: () => ({
    items: [] as ShoppingListDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
      this.loading = true
      this.error = null
      try {
        const response = await api.get<ShoppingListDto[]>('/shoppinglist');
        this.items = response.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to load shopping list');
        console.error('Failed to fetch shopping list:', err);
      } finally {
        this.loading = false
      }
    },

    async addItem(dto: ShoppingListCreateDto) {
      this.error = null
      try {
        // Always appends a new row — the backend is keyed by an auto-increment Id
        // (not by ingredient), so the same ingredient can legitimately appear more
        // than once (e.g. different amounts added for different recipes).
        const res = await api.post<ShoppingListDto>('/shoppinglist', dto);
        this.items.push(res.data);
        return res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to add item to shopping list');
        throw err;
      }
    },

    async removeItem(id: number) {
      this.error = null
      try {
        await api.delete(`/shoppinglist/${id}`);
        this.items = this.items.filter(i => i.id !== id);
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to remove item from shopping list');
        throw err;
      }
    },
  }
});
