import { describe, it, expect, vi, beforeEach } from 'vitest'
import { AxiosError } from 'axios'
import { useIngredientSearch } from '@/composables/useIngredientSearch'
import api from '@/services/api'
import type { IngredientSearchParams, PagedResultDto, RecipeSearchResultDto } from '@/interfaces/ingredientSearch'

vi.mock('@/services/api', () => ({
  default: {
    post: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api, true)

const params: IngredientSearchParams = {
  mandatoryIds: [1, 2],
  optionalIds: [3],
  threshold: 0.5,
}

const page: PagedResultDto<RecipeSearchResultDto> = {
  items: [
    {
      recipeId: 1,
      name: 'Soup',
      optionalMatchCount: 1,
      optionalTotal: 1,
      matchedOptionalIds: [3],
      extraIngredientCount: 0,
      totalIngredientCount: 3,
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
}

describe('useIngredientSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('posts search params and populates results', async () => {
    mockedApi.post.mockResolvedValueOnce({ data: page })

    const { results, loading, error, search } = useIngredientSearch()
    await search(params)

    expect(mockedApi.post).toHaveBeenCalledWith('/recipe/search/ingredients', {
      ...params,
      page: 1,
      pageSize: 20,
    })
    expect(results.value).toEqual(page)
    expect(loading.value).toBe(false)
    expect(error.value).toBeNull()
  })

  it('loadPage re-runs the last search with the requested page', async () => {
    mockedApi.post.mockResolvedValueOnce({ data: page })
    mockedApi.post.mockResolvedValueOnce({ data: { ...page, page: 2 } })

    const { results, loadPage, search } = useIngredientSearch()
    await search(params)
    await loadPage(2)

    expect(mockedApi.post).toHaveBeenLastCalledWith('/recipe/search/ingredients', {
      ...params,
      page: 2,
      pageSize: 20,
    })
    expect(results.value?.page).toBe(2)
  })

  it('loadPage is a no-op if no search has run yet', async () => {
    const { loadPage } = useIngredientSearch()
    await loadPage(2)

    expect(mockedApi.post).not.toHaveBeenCalled()
  })

  it('surfaces the server message on a 400 response', async () => {
    const axiosError = new AxiosError('Bad Request')
    axiosError.response = { data: 'invalid params', status: 400 } as never
    mockedApi.post.mockRejectedValueOnce(axiosError)

    const { results, error, search } = useIngredientSearch()
    await search(params)

    expect(results.value).toBeNull()
    expect(error.value).toBe('invalid params')
  })

  it('clear resets results, error and lastParams', async () => {
    mockedApi.post.mockResolvedValueOnce({ data: page })

    const { results, error, search, clear, loadPage } = useIngredientSearch()
    await search(params)
    clear()

    expect(results.value).toBeNull()
    expect(error.value).toBeNull()

    await loadPage(2)
    expect(mockedApi.post).toHaveBeenCalledTimes(1) // loadPage was a no-op after clear
  })
})
