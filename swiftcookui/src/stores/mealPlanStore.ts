import { defineStore } from 'pinia';
import api from '@/services/api';
import type { MealPlanEntryDto, MealPlanEntryCreateDto } from '@/interfaces/mealPlan';
import { addDays, mondayOf, todayIso } from '@/utils/dates';
import { getErrorMessage } from '@/utils/errors';

// Only the most recent week fetch may update the store (guards against out-of-order responses).
let latestRequest = 0

export const useMealPlanStore = defineStore('mealPlan', {
  state: () => ({
    weekStart: mondayOf(todayIso()),
    entries: [] as MealPlanEntryDto[],
    error: null as string | null,
    loading: false,
  }),

  getters: {
    weekDays: (state) => Array.from({ length: 7 }, (_, i) => addDays(state.weekStart, i)),
  },

  actions: {
    async fetchWeek() {
      const requestId = ++latestRequest
      this.loading = true
      this.error = null
      try {
        const res = await api.get<MealPlanEntryDto[]>('/mealplan', {
          params: { from: this.weekStart, to: addDays(this.weekStart, 6) },
        });
        if (requestId === latestRequest) this.entries = res.data;
      } catch (err: unknown) {
        if (requestId === latestRequest) {
          this.error = getErrorMessage(err, 'Failed to load the meal plan');
          console.error('Failed to fetch meal plan:', err);
        }
      } finally {
        if (requestId === latestRequest) this.loading = false
      }
    },

    async goToWeek(weekStart: string) {
      this.weekStart = mondayOf(weekStart)
      await this.fetchWeek()
    },

    async nextWeek() {
      await this.goToWeek(addDays(this.weekStart, 7))
    },

    async previousWeek() {
      await this.goToWeek(addDays(this.weekStart, -7))
    },

    async thisWeek() {
      await this.goToWeek(todayIso())
    },

    entriesFor(date: string, mealType: string) {
      return this.entries.filter(e => e.date === date && e.mealType === mealType)
    },

    async addEntry(dto: MealPlanEntryCreateDto) {
      this.error = null
      try {
        const res = await api.post<MealPlanEntryDto>('/mealplan', dto);
        await this.fetchWeek()
        return res.data;
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to add to the meal plan');
        throw err;
      }
    },

    async updateServings(entry: MealPlanEntryDto, servings: number) {
      this.error = null
      try {
        await api.put(`/mealplan/${entry.id}`, {
          date: entry.date,
          mealType: entry.mealType,
          recipeId: entry.recipeId,
          servings,
          sortOrder: entry.sortOrder,
        });
        entry.servings = servings
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to update servings');
        throw err;
      }
    },

    async markMade(entry: MealPlanEntryDto) {
      this.error = null
      try {
        const res = await api.post<MealPlanEntryDto>(`/mealplan/${entry.id}/made`);
        entry.cookLogId = res.data.cookLogId
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to mark as made');
        throw err;
      }
    },

    async undoMade(entry: MealPlanEntryDto) {
      this.error = null
      try {
        await api.delete(`/mealplan/${entry.id}/made`);
        entry.cookLogId = null
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to undo');
        throw err;
      }
    },

    async removeEntry(id: number) {
      this.error = null
      try {
        await api.delete(`/mealplan/${id}`);
        this.entries = this.entries.filter(e => e.id !== id)
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to remove from the meal plan');
        throw err;
      }
    },
  }
});
