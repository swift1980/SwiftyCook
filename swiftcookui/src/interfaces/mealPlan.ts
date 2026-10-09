export const MEAL_TYPES = ['Breakfast', 'Lunch', 'Dinner', 'Snack', 'Cocktail'] as const
export type MealType = (typeof MEAL_TYPES)[number]

/** Recipes in this category are cocktails; they can only be planned as the Cocktail meal type. */
export const COCKTAIL_CATEGORY_ID = 1

export interface MealPlanEntryDto {
  id: number
  date: string
  mealType: MealType
  recipeId: number
  recipeName: string
  servings: number
  sortOrder: number
  cookLogId?: number | null
}

export interface MealPlanEntryCreateDto {
  date: string
  mealType: MealType
  recipeId: number
  servings?: number
}

export interface MealPlanEntryUpdateDto {
  date: string
  mealType: MealType
  recipeId: number
  servings: number
  sortOrder: number
}
