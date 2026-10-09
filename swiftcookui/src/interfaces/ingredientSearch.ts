export interface IngredientSearchParams {
  mandatoryIds: number[]
  optionalIds: number[]
  threshold: number
}

export interface CupboardSearchParams {
  ingredientIds: number[]
  includeMissing: boolean
}

export interface RecipeSearchResultDto {
  recipeId: number
  name: string
  image?: string
  optionalMatchCount: number
  optionalTotal: number
  matchedOptionalIds: number[]
  extraIngredientCount: number
  totalIngredientCount: number
  /** Cupboard search only. */
  missingIngredientCount?: number
}

export interface PagedResultDto<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

/** Normalised shape CardGrid already consumes, extended with optional match metadata. */
export interface RecipeCardItem {
  id: number
  name: string
  image?: string
  optionalMatchCount?: number
  optionalTotal?: number
  matchedOptionalIds?: number[]
  missingCount?: number
}
