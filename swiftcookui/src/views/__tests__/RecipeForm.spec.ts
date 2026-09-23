import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import RecipeForm from '@/views/RecipeForm.vue'
import api from '@/services/api'
import type { IngredientDto } from '@/interfaces/ingredient'
import type { IngredientCategoryDto } from '@/interfaces/ingredientCategory'

vi.mock('@/services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api, true)

// Global mock so window.alert() calls in submitForm() don't throw in jsdom.
window.alert = vi.fn()

const categories: IngredientCategoryDto[] = [
  { id: 1, name: 'Protein', parentCategoryId: null, fallbackTypeId: 30 },
  { id: 2, name: 'Produce', parentCategoryId: null, fallbackTypeId: 31 },
  { id: 3, name: 'Sub-Protein', parentCategoryId: 1, fallbackTypeId: null },
]

const ingredients: IngredientDto[] = [
  { id: 100, name: 'Chicken', typeId: 11, typeName: 'Poultry' },
]

function mockApiResponses() {
  mockedApi.get.mockImplementation((url: string) => {
    if (url === '/ingredient') return Promise.resolve({ data: ingredients })
    if (url === '/ingredientcategory') return Promise.resolve({ data: categories })
    if (url === '/category') return Promise.resolve({ data: [] })
    if (url === '/tag') return Promise.resolve({ data: [] })
    if (url === '/tool') return Promise.resolve({ data: [] })
    if (url === '/unit') return Promise.resolve({ data: [] })
    return Promise.resolve({ data: [] })
  })
}

describe('RecipeForm — Ticket 10 (new-ingredient category requirement)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockApiResponses()
  })

  it('does not show a category picker for an ingredient row that exactly matches an existing ingredient', async () => {
    const wrapper = mount(RecipeForm)
    await flushPromises()

    const nameInput = wrapper.find('input[placeholder="Type ingredient name"]')
    await nameInput.setValue('Chicken')

    expect(wrapper.find('.new-ingredient-category').exists()).toBe(false)
  })

  it('shows a category picker (root categories only) for a brand-new ingredient name', async () => {
    const wrapper = mount(RecipeForm)
    await flushPromises()

    const nameInput = wrapper.find('input[placeholder="Type ingredient name"]')
    await nameInput.setValue('Duck')

    const picker = wrapper.find('.new-ingredient-category')
    expect(picker.exists()).toBe(true)
    const optionLabels = picker.findAll('option').map((o) => o.text())
    expect(optionLabels).toContain('Protein')
    expect(optionLabels).toContain('Produce')
    expect(optionLabels).not.toContain('Sub-Protein') // non-root, excluded
  })

  it('blocks submit with an alert when a new ingredient has no category selected', async () => {
    const wrapper = mount(RecipeForm)
    await flushPromises()

    await wrapper.find('input[placeholder="Type ingredient name"]').setValue('Duck')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(window.alert).toHaveBeenCalledWith('Please select a category for each new ingredient before saving.')
    expect(mockedApi.post).not.toHaveBeenCalledWith('/ingredient', expect.anything())
  })

  it('creates a new ingredient with the selected category\'s fallback TypeId (not NULL)', async () => {
    const created: IngredientDto = { id: 999, name: 'Duck', pluralName: '', typeId: 30, typeName: 'Other (Protein)' }
    mockedApi.post.mockImplementation((url: string) => {
      if (url === '/ingredient') return Promise.resolve({ data: created })
      if (url === '/recipe') return Promise.resolve({ data: { id: 1 } })
      return Promise.resolve({ data: {} })
    })

    const wrapper = mount(RecipeForm)
    await flushPromises()

    await wrapper.find('input[placeholder="Type ingredient name"]').setValue('Duck')
    await wrapper.find('.new-ingredient-category').setValue('1') // Protein

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mockedApi.post).toHaveBeenCalledWith('/ingredient', { name: 'Duck', pluralName: '', typeId: 30 })
  })
})
