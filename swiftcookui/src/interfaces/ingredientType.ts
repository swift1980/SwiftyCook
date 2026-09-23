export interface IngredientTypeDto {
  id: number
  name: string
  // Optional here because ingredientStore.ts's getIngredientsGroupedByTypeWithNames
  // getter synthesizes a partial IngredientTypeDto from denormalized fields on
  // IngredientDto (typeId/typeName), which doesn't carry categoryId. Real API
  // responses (GET /ingredienttype) always populate it. Ticket 8 will rework that
  // getter to source real category data instead.
  categoryId?: number
}

export interface IngredientTypeCreateDto {
  name: string
  categoryId?: number
}
