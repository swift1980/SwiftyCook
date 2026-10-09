import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useIngredientStore } from '@/stores/ingredientStore'
import { useIngredientTypeStore } from '@/stores/ingredientTypeStore'
import { getErrorMessage } from '@/utils/errors'
import api from '@/services/api'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}))

const mockedApi = vi.mocked(api, true)
const conflict = (message: string) => ({ message: 'Request failed with status code 409', response: { data: { message } } })

describe('ingredient management stores', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('getErrorMessage prefers the API message over the generic Axios one', () => {
    expect(getErrorMessage(conflict('Cannot delete: used in 2 recipes.'), 'x')).toBe('Cannot delete: used in 2 recipes.')
    expect(getErrorMessage(new Error('boom'), 'x')).toBe('boom')
    expect(getErrorMessage(null, 'fallback')).toBe('fallback')
  })

  it('deleteIngredient removes it locally on success', async () => {
    mockedApi.delete.mockResolvedValueOnce({})
    const store = useIngredientStore()
    store.ingredients = [{ id: 1, name: 'A' }, { id: 2, name: 'B' }]
    await store.deleteIngredient(1)
    expect(store.ingredients.map((i) => i.id)).toEqual([2])
  })

  it('deleteIngredient keeps the row and surfaces the 409 message', async () => {
    mockedApi.delete.mockRejectedValueOnce(conflict("Cannot delete 'A': used in 1 recipe."))
    const store = useIngredientStore()
    store.ingredients = [{ id: 1, name: 'A' }]
    await expect(store.deleteIngredient(1)).rejects.toBeTruthy()
    expect(store.ingredients).toHaveLength(1)
    expect(store.error).toBe("Cannot delete 'A': used in 1 recipe.")
  })

  it('updateIngredient PUTs the dto (incl. isStaple) then refetches', async () => {
    mockedApi.put.mockResolvedValueOnce({})
    mockedApi.get.mockResolvedValueOnce({ data: [{ id: 1, name: 'A', isStaple: true }] })
    const store = useIngredientStore()
    await store.updateIngredient(1, { name: 'A', isStaple: true })
    expect(mockedApi.put).toHaveBeenCalledWith('/ingredient/1', { name: 'A', isStaple: true })
    expect(store.ingredients[0].isStaple).toBe(true)
  })

  it('deleteIngredientType surfaces the 409 message', async () => {
    mockedApi.delete.mockRejectedValueOnce(conflict("Cannot delete type 'Flour': 3 ingredients use it."))
    const store = useIngredientTypeStore()
    store.ingredientTypes = [{ id: 1, name: 'Flour', categoryId: 1 }]
    await expect(store.deleteIngredientType(1)).rejects.toBeTruthy()
    expect(store.ingredientTypes).toHaveLength(1)
    expect(store.error).toContain('3 ingredients')
  })
})
