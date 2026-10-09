import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import ShoppingList from '@/views/ShoppingList.vue'
import api from '@/services/api'
import type { ShoppingListDto } from '@/interfaces/shoppingList'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}))

const mockedApi = vi.mocked(api, true)

const items: ShoppingListDto[] = [
  { id: 1, ingredientName: 'Flour', amount: 500, unitName: 'gram' },
  { id: 2, ingredientName: 'Salt' },
]

function mockGets(list: ShoppingListDto[]) {
  mockedApi.get.mockImplementation(async (url: string) => ({ data: url === '/shoppinglist' ? list : [] }))
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
})

describe('ShoppingList purchase', () => {
  it('hides the Purchased button when the list is empty', async () => {
    mockGets([])
    const wrapper = mount(ShoppingList)
    await flushPromises()

    expect(wrapper.find('.purchased-btn').exists()).toBe(false)
  })

  it('lists what will be moved and does nothing until confirmed', async () => {
    mockGets(items)
    const wrapper = mount(ShoppingList)
    await flushPromises()

    await wrapper.find('.purchased-btn').trigger('click')

    const text = wrapper.find('.dialog').text()
    expect(text).toContain('Flour')
    expect(text).toContain('500 gram')
    expect(text).toContain('Salt')
    expect(mockedApi.post).not.toHaveBeenCalled()

    await wrapper.find('.dialog .cancel').trigger('click')
    expect(wrapper.find('.dialog').exists()).toBe(false)
    expect(mockedApi.post).not.toHaveBeenCalled()
  })

  it('sends the confirmed ids and clears the list', async () => {
    mockGets(items)
    mockedApi.post.mockResolvedValueOnce({})
    const wrapper = mount(ShoppingList)
    await flushPromises()

    await wrapper.find('.purchased-btn').trigger('click')
    await wrapper.find('.dialog .confirm').trigger('click')
    await flushPromises()

    expect(mockedApi.post).toHaveBeenCalledWith('/shoppinglist/purchase', { itemIds: [1, 2] })
    expect(wrapper.find('.shopping-list-items').exists()).toBe(false)
    expect(wrapper.find('.dialog').exists()).toBe(false)
  })

  it('keeps the list, shows the error and resyncs when the purchase fails', async () => {
    mockGets(items)
    mockedApi.post.mockRejectedValueOnce(new Error('conflict'))
    const wrapper = mount(ShoppingList)
    await flushPromises()

    await wrapper.find('.purchased-btn').trigger('click')
    await wrapper.find('.dialog .confirm').trigger('click')
    await flushPromises()

    expect(wrapper.findAll('.shopping-list-item')).toHaveLength(2)
    expect(wrapper.find('.error').exists()).toBe(true)
    expect(mockedApi.get.mock.calls.filter(c => c[0] === '/shoppinglist')).toHaveLength(2)
  })
})
