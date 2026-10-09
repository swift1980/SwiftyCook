import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import RecipeImport from '@/views/RecipeImport.vue'
import { useRecipeImportStore } from '@/stores/recipeImportStore'
import api from '@/services/api'
import type { RecipeImportResponse } from '@/interfaces/recipeImport'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}))

const mockedApi = vi.mocked(api, true)

const preview: RecipeImportResponse = {
  dryRun: true,
  recipes: [
    {
      index: 0, name: 'Pancakes', status: 'needsDecision', errors: [], warnings: ['A recipe named \'Pancakes\' already exists.'],
      newCategories: ['Brunch'], newTags: [], newTools: [],
      ingredients: [
        { name: 'Flour', amount: 200, unit: 'g', resolution: 'exact', ingredientName: 'Flour', unitResolution: 'matched' },
        { name: 'Zucchini', unit: 'splash', resolution: 'unresolved', unitResolution: 'unresolved' },
      ],
    },
    { index: 1, name: 'Bad', status: 'invalid', errors: ['ingredients: required.'], warnings: [], ingredients: [], newCategories: [], newTags: [], newTools: [] },
  ],
}

function mockGets() {
  mockedApi.get.mockImplementation(async (url: string) => {
    if (url === '/ingredient') return { data: [{ id: 1, name: 'Flour' }, { id: 2, name: 'Sugar' }] }
    if (url === '/unit') return { data: [{ id: 3, name: 'gram' }] }
    if (url === '/ingredientcategory') return { data: [{ id: 1, name: 'Vegetables', parentCategoryId: null, fallbackTypeId: 5 }, { id: 2, name: 'Empty', parentCategoryId: null, fallbackTypeId: null }] }
    return { data: [] }
  })
}

async function mountWithPreview() {
  mockGets()
  const wrapper = mount(RecipeImport, { global: { stubs: { RouterLink: true } } })
  await flushPromises()
  const store = useRecipeImportStore()
  store.result = preview
  store.recipes = [{}, {}]
  await flushPromises()
  return { wrapper, store }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
})

describe('RecipeImport view', () => {
  it('shows statuses, errors, warnings and decisions only for unresolved lines', async () => {
    const { wrapper } = await mountWithPreview()
    const text = wrapper.text()

    expect(text).toContain('Needs your decision')
    expect(text).toContain('ingredients: required.')
    expect(text).toContain("A recipe named 'Pancakes' already exists.")
    expect(text).toContain('Will create:')
    expect(wrapper.find('select[aria-label="Existing ingredient for Zucchini"]').exists()).toBe(true)
    expect(wrapper.find('select[aria-label="Existing ingredient for Flour"]').exists()).toBe(false)
    expect(wrapper.find('select[aria-label="Unit for splash"]').exists()).toBe(true)
  })

  it('only offers categories that have a fallback type for new ingredients', async () => {
    const { wrapper } = await mountWithPreview()
    const options = wrapper.find('select[aria-label="Category for new ingredient Zucchini"]').findAll('option').map((o) => o.text())

    expect(options).toContain('Vegetables')
    expect(options).not.toContain('Empty')
  })

  it('disables Import until decisions are re-previewed', async () => {
    const { wrapper, store } = await mountWithPreview()
    store.result = { ...preview, recipes: [{ ...preview.recipes[0], status: 'ready' }, preview.recipes[1]] }
    await flushPromises()

    const importBtn = () => wrapper.find('button.confirm')
    expect(importBtn().attributes('disabled')).toBeUndefined()

    await wrapper.find('select[aria-label="Existing ingredient for Zucchini"]').setValue('2')
    expect(store.decisions.ingredients['zucchini']).toEqual({ ingredientId: 2 })
    expect(importBtn().attributes('disabled')).toBeDefined()
  })

  it('does not render markup from recipe names as HTML', async () => {
    const { wrapper, store } = await mountWithPreview()
    store.result = { ...preview, recipes: [{ ...preview.recipes[1], name: '<img src=x onerror=alert(1)>' }] }
    await flushPromises()

    expect(wrapper.find('img').exists()).toBe(false)
    expect(wrapper.text()).toContain('<img src=x onerror=alert(1)>')
  })
})
