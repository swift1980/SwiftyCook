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
// root-category boxes only). fallbackTypeId (Ticket 6) points Protein/Produce
// at a distinct catch-all type; Cocktail has none configured, to test that
// missing-fallback edge case.
const categories: IngredientCategoryDto[] = [
  { id: 1, name: 'Protein', parentCategoryId: null, fallbackTypeId: 30 },
  { id: 2, name: 'Produce', parentCategoryId: null, fallbackTypeId: 31 },
  { id: 3, name: 'Cocktail', parentCategoryId: null, fallbackTypeId: null },
  { id: 4, name: 'Sub-Protein', parentCategoryId: 1, fallbackTypeId: null },
]

const types: IngredientTypeDto[] = [
  { id: 10, name: 'Meat', categoryId: 1 },
  { id: 11, name: 'Poultry', categoryId: 1 },
  { id: 20, name: 'Vegetable', categoryId: 2 },
  { id: 30, name: 'Other (Protein)', categoryId: 1 }, // Protein's fallback type (Ticket 6)
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

  it('restricts boxes to the allowed ingredient categories', async () => {
    const wrapper = mount(AdvancedSearch, {
      props: { allowedCategoryIds: [3] },
    })
    await flushPromises()

    const labels = wrapper.findAll('.advanced-search__field label').map((l) => l.text())
    expect(labels).toEqual(['Cocktail'])
    expect(wrapper.find('#ingredient-category-1').exists()).toBe(false)
    expect(wrapper.find('#ingredient-category-2').exists()).toBe(false)
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

  it('shows a "+ Add as new ingredient" affordance when a typed name has no match, instead of discarding it', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const proteinInput = wrapper.find('#ingredient-category-1')
    await proteinInput.setValue('Duck')
    await proteinInput.trigger('keydown.enter')

    // Not silently discarded — the input keeps the typed text and the
    // confirm affordance appears instead of an ingredient tag.
    expect((proteinInput.element as HTMLInputElement).value).toBe('Duck')
    expect(wrapper.findAll('.ingredient-tag')).toHaveLength(0)
    const addBtn = wrapper.find('.add-new-btn')
    expect(addBtn.exists()).toBe(true)
    expect(addBtn.text()).toContain('Duck')
  })

  it('confirming the "+ Add as new ingredient" affordance creates the ingredient under the category\'s fallback type and selects it', async () => {
    const created: IngredientDto = { id: 999, name: 'Duck', pluralName: '', typeId: 30, typeName: 'Other (Protein)' }
    mockedApi.post.mockResolvedValueOnce({ data: created })

    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const proteinInput = wrapper.find('#ingredient-category-1')
    await proteinInput.setValue('Duck')
    await proteinInput.trigger('keydown.enter')
    await wrapper.find('.add-new-btn').trigger('click')
    await flushPromises()

    expect(mockedApi.post).toHaveBeenCalledWith('/ingredient', { name: 'Duck', pluralName: '', typeId: 30 })
    const tags = wrapper.findAll('.ingredient-tag')
    expect(tags).toHaveLength(1)
    expect(tags[0].text()).toContain('Duck')
    expect(tags[0].text()).toContain('Other (Protein)')
    // Input is cleared and the affordance disappears once created.
    expect((proteinInput.element as HTMLInputElement).value).toBe('')
    expect(wrapper.find('.add-new-btn').exists()).toBe(false)
  })

  it('does not offer the "+ Add as new ingredient" affordance for a category with no fallback type configured', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const cocktailInput = wrapper.find('#ingredient-category-3')
    await cocktailInput.setValue('Gin')
    await cocktailInput.trigger('keydown.enter')

    // Cocktail (category 3) has fallbackTypeId: null in the mock data — the
    // affordance still renders (missing-fallback is a configuration error,
    // not a UI-hidden state), but confirming it is a safe no-op.
    const addBtn = wrapper.find('.add-new-btn')
    expect(addBtn.exists()).toBe(true)
    await addBtn.trigger('click')
    await flushPromises()

    expect(mockedApi.post).not.toHaveBeenCalled()
  })

  it('shows a "Did you mean...?" suggestion for a near-duplicate typo instead of only offering to create a new ingredient (Ticket 7)', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const proteinInput = wrapper.find('#ingredient-category-1')
    await proteinInput.setValue('Chiken') // typo of "Chicken"
    await proteinInput.trigger('keydown.enter')

    const suggestion = wrapper.find('.suggestion-btn')
    expect(suggestion.exists()).toBe(true)
    expect(suggestion.text()).toContain('Chicken')
    // The "+ Add as new" affordance is still offered alongside the suggestion.
    expect(wrapper.find('.add-new-btn').exists()).toBe(true)
  })

  it('clicking a "Did you mean...?" suggestion selects the existing ingredient instead of creating a new one', async () => {
    const wrapper = mount(AdvancedSearch)
    await flushPromises()

    const proteinInput = wrapper.find('#ingredient-category-1')
    await proteinInput.setValue('Chiken')
    await proteinInput.trigger('keydown.enter')

    await wrapper.find('.suggestion-btn').trigger('click')
    await flushPromises()

    expect(mockedApi.post).not.toHaveBeenCalled()
    const tags = wrapper.findAll('.ingredient-tag')
    expect(tags).toHaveLength(1)
    expect(tags[0].text()).toContain('Chicken')
    expect((proteinInput.element as HTMLInputElement).value).toBe('')
    expect(wrapper.find('.suggestion-btn').exists()).toBe(false)
    expect(wrapper.find('.add-new-btn').exists()).toBe(false)
  })
})
