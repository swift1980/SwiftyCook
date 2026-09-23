export interface IngredientCategoryDto {
  id: number
  name: string
  parentCategoryId: number | null
  // Designated catch-all IngredientType for this category, used by manual
  // ingredient entry (Ticket 6) to type newly-created ingredients without
  // prompting the user to pick a specific type.
  fallbackTypeId: number | null
}
