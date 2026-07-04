export interface IngredientDto {
  id: number
  name: string
  pluralName?: string
  typeId?: number
  typeName?: string
}

export interface IngredientCreateDto {
  name: string
  pluralName?: string
  typeId?: number
}
