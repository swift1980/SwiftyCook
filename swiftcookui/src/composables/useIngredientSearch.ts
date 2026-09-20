import { ref } from 'vue'
import axios from 'axios'
import api from '@/services/api'
import type { IngredientSearchParams, PagedResultDto, RecipeSearchResultDto } from '@/interfaces/ingredientSearch'

export function useIngredientSearch() {
  const results = ref<PagedResultDto<RecipeSearchResultDto> | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  let lastParams: IngredientSearchParams | null = null

  async function search(params: IngredientSearchParams, page = 1) {
    loading.value = true
    error.value = null
    lastParams = params

    try {
      const response = await api.post<PagedResultDto<RecipeSearchResultDto>>(
        '/recipe/search/ingredients',
        { ...params, page, pageSize: 20 }
      )
      results.value = response.data
    } catch (err: unknown) {
      results.value = null
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        error.value = err.response.data ?? 'Invalid search request.'
      } else {
        error.value = 'Failed to load results. Please try again.'
      }
    } finally {
      loading.value = false
    }
  }

  async function loadPage(page: number) {
    if (lastParams) await search(lastParams, page)
  }

  function clear() {
    results.value = null
    error.value = null
    lastParams = null
  }

  return { results, loading, error, search, loadPage, clear }
}
