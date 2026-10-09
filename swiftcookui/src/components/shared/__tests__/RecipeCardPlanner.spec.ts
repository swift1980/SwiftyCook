import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import RecipeCard from '@/components/shared/RecipeCard.vue'
import { guessMealType } from '@/utils/mealType'
import { todayIso } from '@/utils/dates'
import api from '@/services/api'
import type { RecipeDto } from '@/interfaces/recipe'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}))

const mockedApi = vi.mocked(api, true)

const make = (over: Partial<RecipeDto>): RecipeDto => ({
  id: 12, name: 'Pancakes', prepTime: 0, cookTime: 0, yield: 4, categoryIds: [2], categories: ['Breakfast'],
  tags: [], tools: [], ingredients: [], instructions: [], ...over,
})

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  document.body.innerHTML = ''
  mockedApi.post.mockResolvedValue({ data: {} })
  mockedApi.get.mockResolvedValue({ data: [] })
})

describe('guessMealType', () => {
  it('maps meal categories, cocktails and falls back to Dinner', () => {
    expect(guessMealType(make({ categories: ['Lunch'] }))).toBe('Lunch')
    expect(guessMealType(make({ categories: ['dessert', 'Snack'] }))).toBe('Snack')
    expect(guessMealType(make({ categories: ['Dessert'] }))).toBe('Dinner')
    expect(guessMealType(make({ categoryIds: [1], categories: ['Cocktail'] }))).toBe('Cocktail')
  })
})

describe('RecipeCard add to meal planner', () => {
  it('opens the dialog with today, a guessed meal type and the recipe yield', async () => {
    const wrapper = mount(RecipeCard, { props: { recipe: make({}) }, attachTo: document.body })
    await wrapper.find('.plan-btn').trigger('click')

    expect((wrapper.find('input[type=date]').element as HTMLInputElement).value).toBe(todayIso())
    expect((wrapper.find('select[aria-label="Meal type"]').element as HTMLSelectElement).value).toBe('Breakfast')
    expect((wrapper.find('.servings input').element as HTMLInputElement).value).toBe('4')
    expect(wrapper.find('.search').exists()).toBe(false)
  })

  it('posts the chosen date, meal and servings and confirms', async () => {
    const wrapper = mount(RecipeCard, { props: { recipe: make({}) }, attachTo: document.body })
    await wrapper.find('.plan-btn').trigger('click')
    await wrapper.find('input[type=date]').setValue('2026-10-06')
    await wrapper.find('select[aria-label="Meal type"]').setValue('Lunch')
    await wrapper.find('.servings input').setValue(2)
    await wrapper.find('button.add').trigger('click')
    await flushPromises()

    expect(mockedApi.post).toHaveBeenCalledWith('/mealplan', { date: '2026-10-06', mealType: 'Lunch', recipeId: 12, servings: 2 })
    expect(wrapper.find('.planned').text()).toBe('Added to Tuesday lunch (2026-10-06)')
    expect(wrapper.find('[role=dialog][aria-label="Add to meal plan"]').exists()).toBe(false)
  })

  it('fixes cocktails to the Cocktail meal type', async () => {
    const wrapper = mount(RecipeCard, {
      props: { recipe: make({ id: 1, name: 'Margarita', categoryIds: [1], categories: ['Cocktail'], yield: 1 }) },
      attachTo: document.body,
    })
    await wrapper.find('.plan-btn').trigger('click')
    const select = wrapper.find('select[aria-label="Meal type"]')

    expect(select.findAll('option').map(o => o.text())).toEqual(['Cocktail'])
    expect(select.attributes('disabled')).toBeDefined()
  })

  it('never offers Cocktail for non-cocktail recipes', async () => {
    const wrapper = mount(RecipeCard, { props: { recipe: make({}) }, attachTo: document.body })
    await wrapper.find('.plan-btn').trigger('click')

    expect(wrapper.find('select[aria-label="Meal type"]').findAll('option').map(o => o.text()))
      .toEqual(['Breakfast', 'Lunch', 'Dinner', 'Snack'])
  })

  it('shows the API error and keeps the dialog open when adding fails', async () => {
    mockedApi.post.mockRejectedValue({ response: { data: { message: 'Nope' } } })
    const wrapper = mount(RecipeCard, { props: { recipe: make({}) }, attachTo: document.body })
    await wrapper.find('.plan-btn').trigger('click')
    await wrapper.find('button.add').trigger('click')
    await flushPromises()

    expect(wrapper.find('.dialog .error').text()).toBe('Nope')
    expect(wrapper.find('.planned').exists()).toBe(false)
  })
})
