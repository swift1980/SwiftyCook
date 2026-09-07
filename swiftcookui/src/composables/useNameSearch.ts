import { ref } from 'vue'
import api from '@/services/api'
import type { RecipeDto } from '@/interfaces/recipe'

/** Backend-driven recipe name search (GET /recipe/search?q=) */
export function useNameSearch() {
  const results = ref<RecipeDto[] | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function search(q: string) {
    if (!q.trim()) {
      clear()
      return
    }

    loading.value = true
    error.value = null

    try {
      const response = await api.get<RecipeDto[]>('/recipe/search', { params: { q } })
      results.value = response.data
    } catch (err: any) {
      results.value = null
      if (err.response?.status === 400) {
        error.value = err.response.data ?? 'Invalid search request.'
      } else {
        error.value = 'Failed to load results. Please try again.'
      }
    } finally {
      loading.value = false
    }
  }

  function clear() {
    results.value = null
    error.value = null
  }

  return { results, loading, error, search, clear }
}
