export interface IngredientTypeDto {
  id: number
  name: string
  categoryId: number
}

export interface IngredientTypeCreateDto {
  name: string
  categoryId?: number
}
