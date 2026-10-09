import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import CookLogPanel from '@/components/CookLogPanel.vue'
import api from '@/services/api'
import { lastMadeText } from '@/utils/cookLog'
import { todayIso } from '@/utils/dates'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}))
const mockedApi = vi.mocked(api, true)

const log = { id: 5, recipeId: 3, cookedOn: '2026-10-01', servings: 2, notes: 'good' }

function setup(logs = [log]) {
  mockedApi.get.mockResolvedValue({ data: logs })
  mockedApi.post.mockResolvedValue({ data: {} })
  mockedApi.put.mockResolvedValue({ data: {} })
  mockedApi.delete.mockResolvedValue({ data: {} })
  return mount(CookLogPanel, { props: { recipeId: 3, defaultServings: 4 } })
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
})

describe('lastMadeText', () => {
  it('describes recency from the newest date', () => {
    expect(lastMadeText([], '2026-10-10')).toBeNull()
    expect(lastMadeText(['2026-10-10'], '2026-10-10')).toBe('Last made: today')
    expect(lastMadeText(['2026-10-09'], '2026-10-10')).toBe('Last made: yesterday')
    expect(lastMadeText(['2026-09-01', '2026-10-05'], '2026-10-10')).toBe('Last made: 5 days ago')
  })
})

describe('CookLogPanel.vue', () => {
  it('shows history and last made, and defaults the form', async () => {
    const wrapper = setup()
    await flushPromises()
    expect(mockedApi.get).toHaveBeenCalledWith('/cooklog', { params: { recipeId: 3 } })
    expect(wrapper.text()).toContain('Last made:')
    expect(wrapper.text()).toContain('2026-10-01 · 2 servings · good')
    expect((wrapper.find('input[type=date]').element as HTMLInputElement).value).toBe(todayIso())
    expect((wrapper.find('input[type=number]').element as HTMLInputElement).value).toBe('4')
  })

  it('shows "Not made yet" with no logs', async () => {
    const wrapper = setup([])
    await flushPromises()
    expect(wrapper.text()).toContain('Not made yet')
  })

  it('logs as made with trimmed notes', async () => {
    const wrapper = setup([])
    await flushPromises()
    await wrapper.find('input[type=text]').setValue('  tasty  ')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(mockedApi.post).toHaveBeenCalledWith('/cooklog', { recipeId: 3, cookedOn: todayIso(), servings: 4, notes: 'tasty' })
  })

  it('edits an entry', async () => {
    const wrapper = setup()
    await flushPromises()
    await wrapper.findAll('.history button')[0].trigger('click')
    await wrapper.find('input[type=number]').setValue(6)
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(mockedApi.put).toHaveBeenCalledWith('/cooklog/5', { cookedOn: '2026-10-01', servings: 6, notes: 'good' })
  })

  it('requires a second click to delete', async () => {
    const wrapper = setup()
    await flushPromises()
    await wrapper.findAll('.history button')[1].trigger('click')
    expect(mockedApi.delete).not.toHaveBeenCalled()
    await wrapper.find('.history .danger').trigger('click')
    await flushPromises()
    expect(mockedApi.delete).toHaveBeenCalledWith('/cooklog/5')
    expect(wrapper.find('.history').exists()).toBe(false)
  })

  it('rejects invalid servings', async () => {
    const wrapper = setup([])
    await flushPromises()
    await wrapper.find('input[type=number]').setValue(0)
    expect(wrapper.find('form button[type=submit]').attributes('disabled')).toBeDefined()
  })
})
