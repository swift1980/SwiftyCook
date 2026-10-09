import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import MealPlanner from '@/views/MealPlanner.vue'
import { useMealPlanStore } from '@/stores/mealPlanStore'
import api from '@/services/api'
import type { RecipeDto } from '@/interfaces/recipe'
import type { MealPlanEntryDto } from '@/interfaces/mealPlan'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}))

const mockedApi = vi.mocked(api, true)

const recipe = (id: number, name: string, categoryIds: number[], yieldN = 1): RecipeDto => ({
  id, name, prepTime: 0, cookTime: 0, yield: yieldN, categoryIds, categories: [], tags: [], tools: [], ingredients: [], instructions: [],
})

// Like the real API: /recipe excludes cocktails, /cocktail returns only cocktails.
const recipes = [recipe(12, 'Omelette', [4], 4)]
const cocktails = [recipe(1, 'Margarita', [1])]

const entry: MealPlanEntryDto = {
  id: 7, date: '2026-10-06', mealType: 'Dinner', recipeId: 12, recipeName: 'Omelette', servings: 2, sortOrder: 0,
}

function setup(entries: MealPlanEntryDto[] = [entry]) {
  mockedApi.get.mockImplementation(async (url: string) => ({
    data: url === '/recipe' ? recipes : url === '/cocktail' ? cocktails : entries,
  }))
  const store = useMealPlanStore()
  store.weekStart = '2026-10-05'
  return mount(MealPlanner, { attachTo: document.body })
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  document.body.innerHTML = ''
})

describe('MealPlanner.vue', () => {
  it('renders a 7 x 5 grid with entries in their cells', async () => {
    const wrapper = setup()
    await flushPromises()

    expect(wrapper.findAll('.day-head')).toHaveLength(7)
    expect(wrapper.findAll('.meal-head').map(h => h.text())).toEqual(['Breakfast', 'Lunch', 'Dinner', 'Snack', 'Cocktail'])
    expect(wrapper.find('[data-cell="2026-10-06|Dinner"]').text()).toContain('Omelette')
    expect(wrapper.find('[data-cell="2026-10-06|Lunch"]').text()).not.toContain('Omelette')
  })

  it('only offers cocktails in the Cocktail row and excludes them elsewhere', async () => {
    const wrapper = setup([])
    await flushPromises()

    await wrapper.find('[data-cell="2026-10-05|Cocktail"] .add').trigger('click')
    await flushPromises()
    let names = wrapper.findAll('.results li').map(li => li.text())
    expect(names).toEqual(['Margarita'])

    await wrapper.find('.dialog .close').trigger('click')
    await wrapper.find('[data-cell="2026-10-05|Dinner"] .add').trigger('click')
    await flushPromises()
    names = wrapper.findAll('.results li').map(li => li.text())
    expect(names).toEqual(['Omelette'])
  })

  it('adds a searched recipe with its yield as default servings', async () => {
    const wrapper = setup([])
    await flushPromises()
    mockedApi.post.mockResolvedValueOnce({ data: entry })

    await wrapper.find('[data-cell="2026-10-07|Lunch"] .add').trigger('click')
    await wrapper.find('.dialog .search').setValue('omel')
    await wrapper.find('.results button').trigger('click')
    await wrapper.find('.dialog .add').trigger('click')
    await flushPromises()

    expect(mockedApi.post).toHaveBeenCalledWith('/mealplan', {
      date: '2026-10-07', mealType: 'Lunch', recipeId: 12, servings: 4,
    })
    expect(wrapper.find('.dialog').exists()).toBe(false)
  })

  it('removes an entry and edits its servings', async () => {
    const wrapper = setup()
    await flushPromises()
    mockedApi.put.mockResolvedValueOnce({})
    mockedApi.delete.mockResolvedValueOnce({})

    const input = wrapper.find('.entry-servings')
    await input.setValue('6')
    await input.trigger('change')
    await flushPromises()
    expect(mockedApi.put).toHaveBeenCalledWith('/mealplan/7', expect.objectContaining({ servings: 6 }))

    await wrapper.find('.entry .remove').trigger('click')
    await flushPromises()
    expect(mockedApi.delete).toHaveBeenCalledWith('/mealplan/7')
    expect(wrapper.find('.entry').exists()).toBe(false)
  })

  it('shows an error when the dialog add fails and keeps the dialog open', async () => {
    const wrapper = setup([])
    await flushPromises()
    mockedApi.post.mockRejectedValueOnce(new Error('nope'))

    await wrapper.find('[data-cell="2026-10-07|Lunch"] .add').trigger('click')
    await wrapper.find('.results button').trigger('click')
    await wrapper.find('.dialog .add').trigger('click')
    await flushPromises()

    expect(wrapper.find('.dialog .error').exists()).toBe(true)
  })

  it('marks an entry made and undoes it', async () => {
    const wrapper = setup()
    await flushPromises()
    mockedApi.post.mockResolvedValue({ data: { ...entry, cookLogId: 9 } })
    mockedApi.delete.mockResolvedValue({ data: {} })

    await wrapper.find('.entry .made').trigger('click')
    await flushPromises()
    expect(mockedApi.post).toHaveBeenCalledWith('/mealplan/7/made')
    expect(wrapper.find('.entry .made').text()).toContain('Made')

    await wrapper.find('.entry .made').trigger('click')
    await flushPromises()
    expect(mockedApi.delete).toHaveBeenCalledWith('/mealplan/7/made')
    expect(wrapper.find('.entry .made').text()).toContain('Mark made')
  })

  it('disables marking made for future dates', async () => {
    const wrapper = setup([{ ...entry, date: '2999-10-06' }])
    const store = useMealPlanStore()
    store.weekStart = '2999-10-05'
    await store.fetchWeek()
    await flushPromises()
    expect(wrapper.find('.entry .made').attributes('disabled')).toBeDefined()
  })

  it('previews shortages for the visible week, flags mismatches and adds on confirm', async () => {
    const wrapper = setup()
    await flushPromises()
    const lines = [
      { ingredientId: 1, ingredientName: 'Flour', unitId: 10, unitName: 'gram', amount: 500, unitMismatch: true },
      { ingredientId: 5, ingredientName: 'Salt', unitId: 14, unitName: 'sgl', amount: null, unitMismatch: false },
    ]
    mockedApi.post.mockResolvedValue({ data: lines })

    await wrapper.find('.shop-btn').trigger('click')
    await flushPromises()
    expect(mockedApi.post).toHaveBeenCalledWith('/shoppinglist/from-mealplan', { from: '2026-10-05', to: '2026-10-11', dryRun: true })
    const text = wrapper.find('.shop-lines').text()
    expect(text).toContain('Flour – 500 gram')
    expect(text).toContain('check cupboard')
    expect(text).toContain('Salt')

    await wrapper.find('.dialog .confirm').trigger('click')
    await flushPromises()
    expect(mockedApi.post).toHaveBeenLastCalledWith('/shoppinglist/from-mealplan', { from: '2026-10-05', to: '2026-10-11', dryRun: false })
    expect(wrapper.find('.done').text()).toContain('Added 2 items')
  })

  it('says so when there is nothing to add', async () => {
    const wrapper = setup()
    await flushPromises()
    mockedApi.post.mockResolvedValue({ data: [] })

    await wrapper.find('.shop-btn').trigger('click')
    await flushPromises()
    expect(wrapper.find('.dialog').text()).toContain('Nothing to add')
    expect(wrapper.find('.dialog .confirm').attributes('disabled')).toBeDefined()
  })
})