export interface CupboardDto {
  ingredientId: number
  ingredientName: string
  unitId: number
  amount?: number
  unitName?: string
}

export interface CupboardCreateDto {
  ingredientId: number
  unitId: number
  amount?: number
}
