import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import Cocktails from '@/views/Cocktails.vue'
import api from '@/services/api'
import type { RecipeDto } from '@/interfaces/recipe'
import type { PagedResultDto, RecipeSearchResultDto } from '@/interfaces/ingredientSearch'

vi.mock('@/services/api', () => ({
  default: {
    get: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api, true)

const cocktails: RecipeDto[] = [
  { id: 1, name: 'Margarita', prepTime: 5, cookTime: 0, yield: 1, categoryIds: [1], categories: ['Cocktail'], tags: [], tools: [], ingredients: [], instructions: [] },
  { id: 2, name: 'Mojito', prepTime: 5, cookTime: 0, yield: 1, categoryIds: [1], categories: ['Cocktail'], tags: [], tools: [], ingredients: [], instructions: [] },
]

beforeEach(() => {
  setActivePinia(createPinia())
  mockedApi.get.mockReset()
})

describe('Cocktails.vue', () => {
  it('fetches and renders all cocktails on mount (browse-all path)', async () => {
    mockedApi.get.mockResolvedValueOnce({ data: cocktails })

    const wrapper = mount(Cocktails, {
      props: { nameQuery: '', searchResults: null, searchError: null, searchLoading: false },
    })
    await flushPromises()

    expect(mockedApi.get).toHaveBeenCalledWith('/cocktail')
    expect(wrapper.text()).toContain('Margarita')
    expect(wrapper.text()).toContain('Mojito')
  })

  it('filters browse-all results by nameQuery client-side', async () => {
    mockedApi.get.mockResolvedValueOnce({ data: cocktails })

    const wrapper = mount(Cocktails, {
      props: { nameQuery: 'mar', searchResults: null, searchError: null, searchLoading: false },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('Margarita')
    expect(wrapper.text()).not.toContain('Mojito')
  })

  it('renders server ingredient-search results when searchResults is provided', async () => {
    mockedApi.get.mockResolvedValueOnce({ data: [] })

    const searchResults: PagedResultDto<RecipeSearchResultDto> = {
      items: [
        { recipeId: 10, name: 'Old Fashioned', optionalMatchCount: 1, optionalTotal: 2, matchedOptionalIds: [1], extraIngredientCount: 0, totalIngredientCount: 3 },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    }

    const wrapper = mount(Cocktails, {
      props: { nameQuery: '', searchResults, searchError: null, searchLoading: false },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('Old Fashioned')
    expect(wrapper.text()).not.toContain('Margarita')
  })

  it('shows the empty-state message when a search returns zero cocktails', async () => {
    mockedApi.get.mockResolvedValueOnce({ data: [] })

    const searchResults: PagedResultDto<RecipeSearchResultDto> = {
      items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0,
    }

    const wrapper = mount(Cocktails, {
      props: { nameQuery: '', searchResults, searchError: null, searchLoading: false },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('No cocktails matched these ingredients')
  })

  it('shows the search error message and emits retry on button click', async () => {
    mockedApi.get.mockResolvedValueOnce({ data: [] })

    const wrapper = mount(Cocktails, {
      props: { nameQuery: '', searchResults: null, searchError: 'Search failed', searchLoading: false },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('Search failed')
    await wrapper.find('.retry-btn').trigger('click')
    expect(wrapper.emitted('retry')).toBeTruthy()
  })
})
