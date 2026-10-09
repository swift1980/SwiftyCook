export interface CookLogDto {
  id: number
  recipeId: number
  cookedOn: string
  servings: number
  notes?: string | null
}

export interface CookLogCreateDto {
  recipeId: number
  cookedOn: string
  servings: number
  notes?: string | null
}

export interface CookLogUpdateDto {
  cookedOn: string
  servings: number
  notes?: string | null
}
