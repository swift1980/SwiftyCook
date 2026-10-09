import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import api from '@/services/api'
import { useRecipeImportStore } from '@/stores/recipeImportStore'
import type { RecipeImportResponse } from '@/interfaces/recipeImport'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}))

const mockedApi = vi.mocked(api, true)

const nearPreview: RecipeImportResponse = {
  dryRun: true,
  recipes: [{
    index: 0, name: 'Pancakes', status: 'needsDecision', errors: [], warnings: [],
    newCategories: [], newTags: [], newTools: [],
    ingredients: [{
      name: 'Suger', resolution: 'near', suggestedIngredientId: 2, suggestedIngredientName: 'Sugar',
      unitResolution: 'matched',
    }],
  }],
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
})

describe('recipeImportStore.loadFile', () => {
  it('rejects oversized files without calling the API', async () => {
    const store = useRecipeImportStore()
    await store.loadFile('big.json', 2 * 1024 * 1024, '')
    expect(store.error).toMatch(/1 MB/)
    expect(mockedApi.post).not.toHaveBeenCalled()
  })

  it('rejects invalid JSON, empty arrays and more than 50 recipes', async () => {
    const store = useRecipeImportStore()

    await store.loadFile('a.json', 5, '{nope')
    expect(store.error).toMatch(/not valid JSON/)

    await store.loadFile('b.json', 2, '[]')
    expect(store.error).toMatch(/no recipes/)

    await store.loadFile('c.json', 10, JSON.stringify(Array.from({ length: 51 }, () => ({}))))
    expect(store.error).toMatch(/at most 50/)

    expect(mockedApi.post).not.toHaveBeenCalled()
  })

  it('wraps a single recipe and previews with dryRun=true', async () => {
    mockedApi.post.mockResolvedValue({ data: { dryRun: true, recipes: [] } })
    const store = useRecipeImportStore()

    await store.loadFile('one.json', 20, '{"name":"X"}')

    expect(mockedApi.post).toHaveBeenCalledWith(
      '/recipe/import',
      { recipes: [{ name: 'X' }], decisions: expect.any(Object) },
      { params: { dryRun: true } },
    )
  })

  it('defaults near matches to the suggestion and re-resolves the preview', async () => {
    const resolved: RecipeImportResponse = {
      dryRun: true,
      recipes: [{ ...nearPreview.recipes[0], status: 'ready', ingredients: [{ ...nearPreview.recipes[0].ingredients[0], resolution: 'mapped' }] }],
    }
    mockedApi.post.mockResolvedValueOnce({ data: nearPreview }).mockResolvedValueOnce({ data: resolved })
    const store = useRecipeImportStore()

    await store.loadFile('one.json', 20, '[{"name":"Pancakes"}]')

    expect(store.decisions.ingredients['suger']).toEqual({ ingredientId: 2 })
    expect(mockedApi.post).toHaveBeenCalledTimes(2)
    expect(mockedApi.post.mock.calls[1][1]).toMatchObject({ decisions: { ingredients: { suger: { ingredientId: 2 } } } })
    expect(store.importable).toBe(true)
  })
})

describe('recipeImportStore decisions', () => {
  it('marks the preview dirty, and a real import sends dryRun=false with the decisions', async () => {
    mockedApi.post.mockResolvedValue({ data: { dryRun: true, recipes: [] } })
    const store = useRecipeImportStore()
    await store.loadFile('one.json', 20, '[{"name":"Pancakes"}]')

    store.createIngredient('Zucchini', 7)
    store.mapUnit('Splash', 4)
    store.toggle('skip', 1, true)
    store.toggle('importDuplicates', 0, true)
    expect(store.dirty).toBe(true)

    mockedApi.post.mockResolvedValue({ data: { dryRun: false, recipes: [] } })
    await store.runImport()

    const [, body, config] = mockedApi.post.mock.calls.at(-1)!
    expect(config).toEqual({ params: { dryRun: false } })
    expect(body).toMatchObject({
      decisions: {
        ingredients: { zucchini: { createCategoryId: 7 } },
        units: { splash: 4 },
        skip: [1],
        importDuplicates: [0],
      },
    })
    expect(store.dirty).toBe(false)
    expect(store.imported).toBe(true)
  })

  it('un-checking removes the index and clearing a choice removes the decision', () => {
    const store = useRecipeImportStore()
    store.toggle('skip', 3, true)
    store.toggle('skip', 3, false)
    expect(store.decisions.skip).toEqual([])

    store.mapIngredient('Egg', 4)
    store.mapIngredient('Egg', null)
    expect(store.decisions.ingredients).toEqual({})
  })

  it('exposes the API message when the request fails', async () => {
    mockedApi.post.mockRejectedValue({ response: { data: { message: 'Too many recipes' } } })
    const store = useRecipeImportStore()
    await store.loadFile('one.json', 20, '[{"name":"X"}]')
    expect(store.error).toBe('Too many recipes')
  })
})
