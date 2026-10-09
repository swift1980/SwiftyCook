export interface IngredientDto {
  id: number
  name: string
  pluralName?: string
  typeId?: number
  typeName?: string
  isStaple?: boolean
}

export interface IngredientCreateDto {
  name: string
  pluralName?: string
  typeId?: number
  isStaple?: boolean
}
