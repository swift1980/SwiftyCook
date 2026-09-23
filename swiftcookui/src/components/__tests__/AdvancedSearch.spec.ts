import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import AdvancedSearch from '@/components/AdvancedSearch.vue'
import api from '@/services/api'
import type { IngredientDto } from '@/interfaces/ingredient'
import type { IngredientTypeDto } from '@/interfaces/ingredientType'
import type { IngredientCategoryDto } from '@/interfaces/ingredientCategory'

vi.mock('@/services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api, true)

// One root category ("Cocktail") intentionally has no ingredients yet, plus a
// hypothetical non-root category, to verify: (a) empty categories still
// render a box, and (b) non-root categories are excluded (Ticket 8 scope is
// root-category boxes only).
const categories: IngredientCategoryDto[] = [
  { id: 1, name: 'Protein', parentCategoryId: null },
  { id: 2, name: 'Produce', parentCategoryId: null },
  { id: 3, name: 'Cocktail', parentCategoryId: null },
  { id: 4, name: 'Sub-Protein', parentCategoryId: 1 },
]

const types: IngredientTypeDto[] = [
  { id: 10, name: 'Meat', categoryId: 1 },
  { id: 11, name: 'Poultry', categoryId: 1 },
  { id: 20, name: 'Vegetable', categoryId: 2 },
]

const ingredients: IngredientDto[] = [
  { id: 100, name: 'Beef', typeId: 10, typeName: 'Meat' },
  { id: 101, name: 'Chicken', typeId: 11, typeName: 'Poultry' },
  { id: 200, name: 'Carrot', typeId: 20, typeName: 'Vegetable' },
]

function mockApiResponses() {
  mockedApi.get.mockImplementation((url: string) => {
    if (url === '/ingredient') return Promise.resolve({ data: ingredients })
    if (url === '/ingredienttype') return Promise.resolve({ data: types })
    if (url === '/ingredientcategory') return Promise.resolve({ data: categories })
    return Promise.resolve({ data: [] })
  })
}

describe('AdvancedSearch', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockApiResponses()
  })

  it('renders one box per root IngredientCategory, not per IngredientType', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const labels = wrapper.findAll('.advanced-search__field label').map((l) => l.text())
    // 3 root categories (Protein, Produce, Cocktail) — not 3 types, and not
    // the non-root "Sub-Protein" category.
    expect(labels).toEqual(['Protein', 'Produce', 'Cocktail'])
  })

  it("a category box's datalist options span all of that category's child IngredientTypes", async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const proteinOptions = wrapper
      .find('#ingredient-options-1')
      .findAll('option')
      .map((o) => o.attributes('value'))
    // Beef (Meat) and Chicken (Poultry) both surface under the single
    // Protein category box.
    expect(proteinOptions).toEqual(['Beef', 'Chicken'])
  })

  it('renders a box for categories with no ingredients yet (fixed box count)', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    expect(wrapper.find('#ingredient-category-3').exists()).toBe(true)
    expect(wrapper.find('#ingredient-options-3').findAll('option')).toHaveLength(0)
  })

  it('selecting an ingredient groups it under its root category box and labels it with its specific IngredientType', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const proteinInput = wrapper.find('#ingredient-category-1')
    await proteinInput.setValue('Chicken')
    await proteinInput.trigger('keydown.enter')

    const tags = wrapper.findAll('.ingredient-tag')
    expect(tags).toHaveLength(1)
    expect(tags[0].text()).toContain('Chicken')
    expect(tags[0].text()).toContain('Poultry')
  })

  it('does not add an ingredient typed into the wrong category box', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    // "Carrot" belongs to Produce (category 2), typed into Protein's box (1).
    const proteinInput = wrapper.find('#ingredient-category-1')
    await proteinInput.setValue('Carrot')
    await proteinInput.trigger('keydown.enter')

    expect(wrapper.findAll('.ingredient-tag')).toHaveLength(0)
  })
})
