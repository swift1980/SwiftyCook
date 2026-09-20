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
  
  getters: {
    /** Filters the full recipe list by the ingredient-name tags (name search is server-side, see useNameSearch). */
    filterByIngredients: (state) => (ingList: string[], matchMode: 'and' | 'or' = 'and') => {
      if (!ingList.length) return state.recipes

      const matchesIngredient = (recipe: RecipeDto, query: string) =>
        recipe.ingredients.some(ing =>
          ing.ingredientName.toLowerCase().includes(query.toLowerCase())
        )

      return state.recipes.filter(recipe =>
        matchMode === 'or'
          ? ingList.some(q => matchesIngredient(recipe, q))
          : ingList.every(q => matchesIngredient(recipe, q))
      )
    }
  },

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
