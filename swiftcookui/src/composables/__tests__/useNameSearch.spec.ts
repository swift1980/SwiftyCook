import { describe, it, expect, vi, beforeEach } from 'vitest'
import { AxiosError } from 'axios'
import { useNameSearch } from '@/composables/useNameSearch'
import api from '@/services/api'
import type { RecipeDto } from '@/interfaces/recipe'

vi.mock('@/services/api', () => ({
  default: {
    get: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api, true)

describe('useNameSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('does nothing (and clears state) when the query is blank', async () => {
    const { results, error, search } = useNameSearch()

    await search('   ')

    expect(mockedApi.get).not.toHaveBeenCalled()
    expect(results.value).toBeNull()
    expect(error.value).toBeNull()
  })

  it('populates results on a successful search', async () => {
    const recipes = [{ id: 1, name: 'Pancakes' }] as unknown as RecipeDto[]
    mockedApi.get.mockResolvedValueOnce({ data: recipes })

    const { results, loading, error, search } = useNameSearch()
    await search('pan')

    expect(mockedApi.get).toHaveBeenCalledWith('/recipe/search', { params: { q: 'pan' } })
    expect(results.value).toEqual(recipes)
    expect(loading.value).toBe(false)
    expect(error.value).toBeNull()
  })

  it('surfaces the server message on a 400 response', async () => {
    const axiosError = new AxiosError('Bad Request')
    axiosError.response = { data: 'q is required', status: 400 } as never
    mockedApi.get.mockRejectedValueOnce(axiosError)

    const { results, error, search } = useNameSearch()
    await search('x')

    expect(results.value).toBeNull()
    expect(error.value).toBe('q is required')
  })

  it('falls back to a generic message for non-400 errors', async () => {
    mockedApi.get.mockRejectedValueOnce(new Error('network down'))

    const { results, error, search } = useNameSearch()
    await search('x')

    expect(results.value).toBeNull()
    expect(error.value).toBe('Failed to load results. Please try again.')
  })

  it('clear resets results and error', async () => {
    const axiosError = new AxiosError('Bad Request')
    axiosError.response = { data: 'q is required', status: 400 } as never
    mockedApi.get.mockRejectedValueOnce(axiosError)

    const { results, error, search, clear } = useNameSearch()
    await search('x')
    clear()

    expect(results.value).toBeNull()
    expect(error.value).toBeNull()
  })
})
