import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { mount, flushPromises } from '@vue/test-utils'
import CardGrid from '@/components/shared/CardGrid.vue'
import api from '@/services/api'
import type { RecipeDto } from '@/interfaces/recipe'

vi.mock('@/services/api', () => ({
  default: {
    get: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api, true)

beforeEach(() => setActivePinia(createPinia()))

const recipe: RecipeDto = {
  id: 3,
  name: 'Mojito',
  description: 'Refreshing Cuban cocktail.',
  prepTime: 5,
  cookTime: 0,
  yield: 1,
  categoryIds: [1],
  categories: ['Cocktail'],
  tags: [],
  tools: [],
  ingredients: [
    {
      recipeId: 3,
      ingredientId: 28,
      ingredientName: 'White rum',
      amount: 2,
      unit: { id: 5, name: 'ounce', abbreviation: 'oz' },
      position: 1,
    },
  ],
  instructions: [{ position: 1, step: 'Add rum.' }],
}

describe('CardGrid', () => {
  it('loads and displays full recipe details when a card is selected', async () => {
    mockedApi.get.mockResolvedValueOnce({ data: recipe })

    const wrapper = mount(CardGrid, {
      props: {
        items: [{ id: 3, name: 'Mojito' }],
      },
    })

    await wrapper.find('.card').trigger('click')
    await flushPromises()

    expect(mockedApi.get).toHaveBeenCalledWith('/recipe/3')
    expect(wrapper.text()).toContain('White rum')
    expect(wrapper.text()).toContain('Add rum.')
  })
})
