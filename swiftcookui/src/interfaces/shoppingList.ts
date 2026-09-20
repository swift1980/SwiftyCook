export interface ShoppingListDto {
  id: number
  ingredientName: string
  amount?: number
  unitName?: string
}

export interface ShoppingListCreateDto {
  ingredientId: number
  unitId: number
  amount?: number
}
