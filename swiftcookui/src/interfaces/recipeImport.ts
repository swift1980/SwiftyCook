export type ImportStatus =
  | 'invalid'
  | 'skipped'
  | 'needsDecision'
  | 'duplicate'
  | 'ready'
  | 'imported'
  | 'failed'

export interface ImportLinePreview {
  name: string
  amount?: number | null
  unit?: string | null
  note?: string | null
  resolution: 'exact' | 'mapped' | 'create' | 'near' | 'unresolved'
  ingredientId?: number | null
  ingredientName?: string | null
  suggestedIngredientId?: number | null
  suggestedIngredientName?: string | null
  unitResolution: 'none' | 'matched' | 'mapped' | 'unresolved'
  unitId?: number | null
  unitName?: string | null
}

export interface RecipeImportItem {
  index: number
  name?: string | null
  status: ImportStatus
  errors: string[]
  warnings: string[]
  ingredients: ImportLinePreview[]
  newCategories: string[]
  newTags: string[]
  newTools: string[]
  recipeId?: number | null
}

export interface RecipeImportResponse {
  dryRun: boolean
  recipes: RecipeImportItem[]
}

export interface IngredientDecision {
  ingredientId?: number
  createCategoryId?: number
}

export interface ImportDecisions {
  ingredients: Record<string, IngredientDecision>
  units: Record<string, number>
  skip: number[]
  importDuplicates: number[]
}

// Limits from docs/recipe-import-format.md (also enforced by the API).
export const IMPORT_MAX_FILE_BYTES = 1024 * 1024
export const IMPORT_MAX_RECIPES = 50
