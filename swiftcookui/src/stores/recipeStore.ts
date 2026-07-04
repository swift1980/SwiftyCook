import { defineStore } from 'pinia';
import api from '@/services/api';
import type { RecipeCreateDto } from "../interfaces/recipe";
import type { RecipeDto } from "../interfaces/recipe";
import type { RecipeIngredientCreateDto } from "../interfaces/recipeingredient";
import type { RecipeInstructionCreateDto } from "../interfaces/recipeinstruction";
import type { RecipeIngredientDto } from "../interfaces/recipeingredient";
import type { RecipeInstructionDto } from "../interfaces/recipeinstruction";

export const useRecipeStore = defineStore('recipe', {
  state: () => ({
    recipes: [] as RecipeDto[],
    recipe: null as RecipeDto | null,
    error: null as string | null,
    loading: false,
  }),
  
  getters: {
    getFilteredRecipes: (state) => (nameQ: string, ingList: string[], matchMode: 'and' | 'or' = 'and') => {
      return state.recipes.filter(recipe => {
        // Match recipe name
        const nameMatch = nameQ
          ? recipe.name.toLowerCase().includes(nameQ.toLowerCase())
          : true

        const matchesIngredient = (query: string) =>
          recipe.ingredients.some(ing =>
            ing.ingredientName.toLowerCase().includes(query.toLowerCase())
          )

        // Match any ingredient
        const ingredientMatch = ingList.length
          ? (matchMode === 'or'
            ? ingList.some(matchesIngredient)
            : ingList.every(matchesIngredient))
            : true

        return nameMatch && ingredientMatch
      })
    }
  },

  actions: {
    async fetchAllRecipes() {
		this.loading = true
      try {
        const response = await api.get<RecipeDto[]>('/recipe');
        this.recipes = response.data;
      } catch (err: any) {
		  this.error = err || 'Failed to load recipes'
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
      } catch (err: any) {
        this.error = err || 'Failed to load recipe'
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
      } catch (err: any) {
        this.error = err || 'Failed to create recipe'
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
