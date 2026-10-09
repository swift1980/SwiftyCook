export interface ShoppingListDto {
  id: number
  ingredientName: string
  amount?: number
  unitName?: string
  source?: string | null
}

export interface ShoppingListGenerationLineDto {
  ingredientId: number
  ingredientName: string
  unitId: number
  unitName: string
  amount?: number | null
  unitMismatch: boolean
}

export interface ShoppingListCreateDto {
  ingredientId: number
  unitId: number
  amount?: number
}
