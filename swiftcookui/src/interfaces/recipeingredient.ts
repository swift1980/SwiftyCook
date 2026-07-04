import type { UnitDto } from './unit'

export interface RecipeIngredientDto {
  recipeId: number
  ingredientId: number
  ingredientName: string
  amount: number | null
  unit?: UnitDto
  position: number
}

export interface RecipeIngredientCreateDto {
  ingredientId: number
  amount: number | null
  unitId: number | null
  position: number
}