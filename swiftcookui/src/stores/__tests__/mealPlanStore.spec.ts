import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useMealPlanStore } from '@/stores/mealPlanStore'
import api from '@/services/api'
import type { MealPlanEntryDto } from '@/interfaces/mealPlan'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}))

const mockedApi = vi.mocked(api, true)

const entry: MealPlanEntryDto = {
  id: 1, date: '2026-10-06', mealType: 'Dinner', recipeId: 12, recipeName: 'Omelette', servings: 2, sortOrder: 0,
}

describe('mealPlanStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('fetches the displayed Monday-to-Sunday range', async () => {
    mockedApi.get.mockResolvedValueOnce({ data: [entry] })
    const store = useMealPlanStore()
    store.weekStart = '2026-10-05'

    await store.fetchWeek()

    expect(mockedApi.get).toHaveBeenCalledWith('/mealplan', { params: { from: '2026-10-05', to: '2026-10-11' } })
    expect(store.entries).toEqual([entry])
    expect(store.weekDays).toHaveLength(7)
  })

  it('navigates between weeks and snaps to Monday', async () => {
    mockedApi.get.mockResolvedValue({ data: [] })
    const store = useMealPlanStore()
    store.weekStart = '2026-10-05'

    await store.nextWeek()
    expect(store.weekStart).toBe('2026-10-12')
    await store.previousWeek()
    await store.previousWeek()
    expect(store.weekStart).toBe('2026-09-28')
    await store.goToWeek('2026-10-08')
    expect(store.weekStart).toBe('2026-10-05')
  })

  it('reloads after adding and drops removed entries', async () => {
    mockedApi.post.mockResolvedValueOnce({ data: entry })
    mockedApi.get.mockResolvedValueOnce({ data: [entry] })
    const store = useMealPlanStore()

    await store.addEntry({ date: '2026-10-06', mealType: 'Dinner', recipeId: 12 })
    expect(mockedApi.get).toHaveBeenCalledTimes(1)
    expect(store.entriesFor('2026-10-06', 'Dinner')).toHaveLength(1)
    expect(store.entriesFor('2026-10-06', 'Lunch')).toHaveLength(0)

    mockedApi.delete.mockResolvedValueOnce({})
    await store.removeEntry(1)
    expect(mockedApi.delete).toHaveBeenCalledWith('/mealplan/1')
    expect(store.entries).toEqual([])
  })

  it('ignores a stale response that arrives after a newer week was requested', async () => {
    let resolveOld!: (v: { data: MealPlanEntryDto[] }) => void
    mockedApi.get
      .mockImplementationOnce(() => new Promise(r => { resolveOld = r }))
      .mockResolvedValueOnce({ data: [] })
    const store = useMealPlanStore()
    store.weekStart = '2026-10-05'

    const first = store.fetchWeek()
    await store.nextWeek()
    resolveOld({ data: [entry] })
    await first

    expect(store.weekStart).toBe('2026-10-12')
    expect(store.entries).toEqual([])
    expect(store.loading).toBe(false)
  })

  it('updates servings with the full entry and surfaces errors', async () => {
    mockedApi.put.mockResolvedValueOnce({})
    const store = useMealPlanStore()
    const e = { ...entry }

    await store.updateServings(e, 5)
    expect(mockedApi.put).toHaveBeenCalledWith('/mealplan/1', {
      date: '2026-10-06', mealType: 'Dinner', recipeId: 12, servings: 5, sortOrder: 0,
    })
    expect(e.servings).toBe(5)

    mockedApi.put.mockRejectedValueOnce(new Error('boom'))
    await expect(store.updateServings(e, 6)).rejects.toThrow()
    expect(e.servings).toBe(5)
    expect(store.error).toBeTruthy()
  })
})
