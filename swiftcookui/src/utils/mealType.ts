import { COCKTAIL_CATEGORY_ID, MEAL_TYPES, type MealType } from '@/interfaces/mealPlan'
import type { RecipeDto } from '@/interfaces/recipe'

/**
 * Best-guess meal type from a recipe's categories. Cocktails are always Cocktail;
 * Breakfast/Lunch/Dinner/Snack categories map to themselves; anything else
 * (e.g. Dessert) defaults to Dinner.
 */
export function guessMealType(recipe: Pick<RecipeDto, 'categoryIds' | 'categories'>): MealType {
  if ((recipe.categoryIds ?? []).includes(COCKTAIL_CATEGORY_ID)) return 'Cocktail'
  for (const name of recipe.categories ?? []) {
    const match = MEAL_TYPES.find(t => t !== 'Cocktail' && t.toLowerCase() === name.trim().toLowerCase())
    if (match) return match
  }
  return 'Dinner'
}

/** e.g. "Added to Tuesday dinner" */
export function addedMessage(date: string, mealType: MealType, weekday: string): string {
  return `Added to ${weekday} ${mealType.toLowerCase()} (${date})`
}
