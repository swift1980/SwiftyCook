import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useCupboardStore } from '@/stores/cupboardStore'
import api from '@/services/api'
import type { CupboardDto } from '@/interfaces/cupboard'

vi.mock('@/services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api, true)

describe('cupboardStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('fetchAll populates items on success', async () => {
    const items: CupboardDto[] = [
      { ingredientId: 1, ingredientName: 'Milk', amount: 1, unitName: 'litre' },
    ]
    mockedApi.get.mockResolvedValueOnce({ data: items })

    const store = useCupboardStore()
    await store.fetchAll()

    expect(mockedApi.get).toHaveBeenCalledWith('/cupboard')
    expect(store.items).toEqual(items)
    expect(store.error).toBeNull()
    expect(store.loading).toBe(false)
  })

  it('fetchAll sets error message on failure', async () => {
    mockedApi.get.mockRejectedValueOnce(new Error('network down'))

    const store = useCupboardStore()
    await store.fetchAll()

    expect(store.items).toEqual([])
    expect(store.error).toBe('network down')
    expect(store.loading).toBe(false)
  })

  it('addItem appends a new item', async () => {
    const created: CupboardDto = { ingredientId: 2, ingredientName: 'Eggs', amount: 6, unitName: 'each' }
    mockedApi.post.mockResolvedValueOnce({ data: created })

    const store = useCupboardStore()
    await store.addItem({ ingredientId: 2, unitId: 1, amount: 6 })

    expect(mockedApi.post).toHaveBeenCalledWith('/cupboard', { ingredientId: 2, unitId: 1, amount: 6 })
    expect(store.items).toEqual([created])
  })

  it('addItem replaces an existing entry for the same ingredient', async () => {
    const store = useCupboardStore()
    store.items = [{ ingredientId: 2, ingredientName: 'Eggs', amount: 6, unitName: 'each' }]

    const updated: CupboardDto = { ingredientId: 2, ingredientName: 'Eggs', amount: 12, unitName: 'each' }
    mockedApi.post.mockResolvedValueOnce({ data: updated })

    await store.addItem({ ingredientId: 2, unitId: 1, amount: 12 })

    expect(store.items).toEqual([updated])
  })

  it('removeItem removes the matching item', async () => {
    const store = useCupboardStore()
    store.items = [
      { ingredientId: 1, ingredientName: 'Milk', amount: 1, unitName: 'litre' },
      { ingredientId: 2, ingredientName: 'Eggs', amount: 6, unitName: 'each' },
    ]
    mockedApi.delete.mockResolvedValueOnce({})

    await store.removeItem(1)

    expect(mockedApi.delete).toHaveBeenCalledWith('/cupboard/1')
    expect(store.items).toEqual([{ ingredientId: 2, ingredientName: 'Eggs', amount: 6, unitName: 'each' }])
  })

  it('removeItem sets error message on failure and keeps items unchanged', async () => {
    const store = useCupboardStore()
    const items: CupboardDto[] = [{ ingredientId: 1, ingredientName: 'Milk', amount: 1, unitName: 'litre' }]
    store.items = [...items]
    mockedApi.delete.mockRejectedValueOnce(new Error('boom'))

    await expect(store.removeItem(1)).rejects.toThrow('boom')

    expect(store.items).toEqual(items)
    expect(store.error).toBe('boom')
  })
})
