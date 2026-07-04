import type { RecipeIngredientDto } from './recipeingredient'
import type { RecipeInstructionDto } from './recipeinstruction'
import type { RecipeIngredientCreateDto } from './recipeingredient'
import type { RecipeInstructionCreateDto } from './recipeinstruction'

export interface RecipeDto {
  id: number
  name: string
  description?: string
  image?: string
  prepTime: number
  cookTime: number
  yield: number
  categoryIds: number[]
  categories: string[]
  tags: string[]
  tools: string[]
  ingredients: RecipeIngredientDto[]
  instructions: RecipeInstructionDto[]
}

export interface RecipeCreateDto {
  name: string
  description?: string
  image?: string
  prepTime: number
  cookTime: number
  yield: number
  categoryIds: number[]
  tagIds: number[]
  toolIds: number[]
  ingredients: RecipeIngredientCreateDto[]
  instructions: RecipeInstructionCreateDto[]
}