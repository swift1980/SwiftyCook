import { defineStore } from 'pinia';
import api from '@/services/api';
import type { RecipeCreateDto } from "../interfaces/recipe";
import type { RecipeDto } from "../interfaces/recipe";
import { getErrorMessage } from '@/utils/errors';

export const useRecipeStore = defineStore('recipe', {
  state: () => ({
    recipes: [] as RecipeDto[],
    recipe: null as RecipeDto | null,
    error: null as string | null,
    loading: false,
  }),
  
  actions: {
    async fetchAllRecipes() {
		this.loading = true
      try {
        const response = await api.get<RecipeDto[]>('/recipe');
        this.recipes = response.data;
      } catch (err: unknown) {
		  this.error = getErrorMessage(err, 'Failed to load recipes')
        console.error('Failed to fetch recipes:', err);
      } finally {
		  this.loading = false
	  }
    },
	async fetchById(id: number) {
      this.loading = true
      this.error = null
      try {
        const res = await api.get<RecipeDto>(`/recipe/${id}`)
        this.recipe = res.data
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to load recipe')
      } finally {
        this.loading = false
      }
    },
    async createRecipe(dto: RecipeCreateDto) {
      this.error = null
      try {
        const res = await api.post<RecipeDto>('/recipe', dto)
        this.recipes.push(res.data) // add to local list
        return res.data
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'Failed to create recipe')
        throw err
      }
    },
	
	reset() {
      this.recipe = null;
      this.recipes = [];
      this.error = null;
      this.loading = false;
    },
  }
});
