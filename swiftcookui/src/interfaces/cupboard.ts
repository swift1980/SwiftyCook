export interface CupboardDto {
  ingredientId: number
  ingredientName: string
  amount?: number
  unitName?: string
}

export interface CupboardCreateDto {
  ingredientId: number
  unitId: number
  amount?: number
}
