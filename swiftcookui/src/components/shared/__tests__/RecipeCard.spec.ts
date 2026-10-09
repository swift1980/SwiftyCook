import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import RecipeCard from '@/components/shared/RecipeCard.vue'
import type { RecipeDto } from '@/interfaces/recipe'

const recipe: RecipeDto = {
  id: 1,
  name: 'Stew',
  description: '',
  prepTime: 0,
  cookTime: 0,
  yield: 2,
  categoryIds: [],
  categories: [],
  tags: [],
  tools: [],
  ingredients: [
    { recipeId: 1, ingredientId: 1, ingredientName: 'onions', amount: 2, unit: { id: 1, name: 'sgl', abbreviation: '' }, position: 1, note: 'finely chopped' },
    { recipeId: 1, ingredientId: 2, ingredientName: 'Salt', amount: null, position: 2, note: 'to taste' },
    { recipeId: 1, ingredientId: 3, ingredientName: 'Water', amount: 500, unit: { id: 7, name: 'millilitre', abbreviation: 'ml' }, position: 3 },
  ],
  instructions: [],
}

describe('RecipeCard ingredient notes', () => {
  it('shows the note after the ingredient and omits a missing amount', () => {
    const items = mount(RecipeCard, { props: { recipe } }).findAll('li').map((li) => li.text().replace(/\s+/g, ' '))

    expect(items).toEqual(['2 onions, finely chopped', 'Salt, to taste', '500 ml Water'])
  })
})

describe('RecipeCard source', () => {
  it('links an http(s) source and shows the author', () => {
    const wrapper = mount(RecipeCard, {
      props: { recipe: { ...recipe, sourceUrl: 'https://example.com/p', sourceTitle: 'Great Pancakes', sourceAuthor: 'Jo' } },
    })
    const link = wrapper.find('.source a')
    expect(link.attributes('href')).toBe('https://example.com/p')
    expect(link.text()).toBe('Great Pancakes')
    expect(wrapper.find('.source').text()).toContain('by Jo')
  })

  it('never renders a non-http source URL as a link', () => {
    const wrapper = mount(RecipeCard, { props: { recipe: { ...recipe, sourceUrl: 'javascript:alert(1)' } } })
    expect(wrapper.find('.source a').exists()).toBe(false)
  })

  it('omits the source line when none is set', () => {
    expect(mount(RecipeCard, { props: { recipe } }).find('.source').exists()).toBe(false)
  })
})
