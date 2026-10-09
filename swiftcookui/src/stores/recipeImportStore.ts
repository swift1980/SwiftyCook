import { defineStore } from 'pinia'
import api from '@/services/api'
import {
  IMPORT_MAX_FILE_BYTES,
  IMPORT_MAX_RECIPES,
  type ImportDecisions,
  type RecipeImportResponse,
} from '@/interfaces/recipeImport'
import { getErrorMessage } from '@/utils/errors'

const emptyDecisions = (): ImportDecisions => ({
  ingredients: {},
  units: {},
  skip: [],
  importDuplicates: [],
})

// Decision keys are lower-cased; the API matches them case-insensitively.
export const decisionKey = (name: string) => name.trim().toLowerCase()

export const useRecipeImportStore = defineStore('recipeImport', {
  state: () => ({
    fileName: '' as string,
    recipes: [] as unknown[],
    decisions: emptyDecisions(),
    result: null as RecipeImportResponse | null,
    // True when decisions changed after the last preview, so the preview is out of date.
    dirty: false,
    loading: false,
    error: null as string | null,
  }),

  getters: {
    importable: (state) =>
      !!state.result && state.result.recipes.some((r) => r.status === 'ready'),
    imported: (state) =>
      !!state.result && !state.result.dryRun,
  },

  actions: {
    reset() {
      this.fileName = ''
      this.recipes = []
      this.decisions = emptyDecisions()
      this.result = null
      this.dirty = false
      this.error = null
    },

    // Parses the uploaded text and applies the documented limits before anything is sent.
    async loadFile(name: string, size: number, text: string) {
      this.reset()
      this.fileName = name

      if (size > IMPORT_MAX_FILE_BYTES) {
        this.error = 'The file is larger than 1 MB.'
        return
      }

      let parsed: unknown
      try {
        parsed = JSON.parse(text)
      } catch {
        this.error = 'The file is not valid JSON.'
        return
      }

      const list = Array.isArray(parsed) ? parsed : [parsed]
      if (list.length === 0) {
        this.error = 'The file contains no recipes.'
        return
      }
      if (list.length > IMPORT_MAX_RECIPES) {
        this.error = `A file may contain at most ${IMPORT_MAX_RECIPES} recipes.`
        return
      }

      this.recipes = list
      await this.preview()
    },

    // Asks the API to resolve names without saving anything.
    async preview() {
      await this.send(true)
    },

    async runImport() {
      await this.send(false)
    },

    async send(dryRun: boolean) {
      this.loading = true
      this.error = null
      try {
        const res = await api.post<RecipeImportResponse>('/recipe/import', {
          recipes: this.recipes,
          decisions: this.decisions,
        }, { params: { dryRun } })
        this.result = res.data
        this.dirty = false
        if (dryRun && this.applySuggestions()) {
          // Re-resolve once so the preview reflects the defaulted near-match decisions.
          const second = await api.post<RecipeImportResponse>('/recipe/import', {
            recipes: this.recipes,
            decisions: this.decisions,
          }, { params: { dryRun: true } })
          this.result = second.data
        }
      } catch (err: unknown) {
        this.error = getErrorMessage(err, 'The import failed')
      } finally {
        this.loading = false
      }
    },

    // Near matches default to "use the existing ingredient" until the user changes them.
    applySuggestions(): boolean {
      let changed = false
      for (const recipe of this.result?.recipes ?? []) {
        for (const line of recipe.ingredients) {
          const key = decisionKey(line.name)
          if (line.resolution === 'near' && line.suggestedIngredientId && !this.decisions.ingredients[key]) {
            this.decisions.ingredients[key] = { ingredientId: line.suggestedIngredientId }
            changed = true
          }
        }
      }
      return changed
    },

    mapIngredient(name: string, ingredientId: number | null) {
      const key = decisionKey(name)
      if (ingredientId == null) delete this.decisions.ingredients[key]
      else this.decisions.ingredients[key] = { ingredientId }
      this.dirty = true
    },

    createIngredient(name: string, createCategoryId: number | null) {
      const key = decisionKey(name)
      if (createCategoryId == null) delete this.decisions.ingredients[key]
      else this.decisions.ingredients[key] = { createCategoryId }
      this.dirty = true
    },

    mapUnit(name: string, unitId: number | null) {
      const key = decisionKey(name)
      if (unitId == null) delete this.decisions.units[key]
      else this.decisions.units[key] = unitId
      this.dirty = true
    },

    toggle(list: 'skip' | 'importDuplicates', index: number, on: boolean) {
      const current = this.decisions[list].filter((i) => i !== index)
      this.decisions[list] = on ? [...current, index] : current
      this.dirty = true
    },
  },
})
