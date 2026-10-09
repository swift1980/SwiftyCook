import { ref } from 'vue'
import axios from 'axios'
import api from '@/services/api'
import type { CupboardSearchParams, IngredientSearchParams, PagedResultDto, RecipeSearchResultDto } from '@/interfaces/ingredientSearch'

// Ticket 5: Cocktails reuse this same mandatory/optional/threshold search
// against their own endpoint (/cocktail/search/ingredients), since Cocktails
// are Recipes filtered server-side by category.
// Ticket 13: the same composable also runs the "recipes I can make" cupboard search.
export function useIngredientSearch(
  endpoint: string = '/recipe/search/ingredients',
  cupboardEndpoint: string = endpoint.replace(/ingredients$/, 'cupboard'),
) {
  const results = ref<PagedResultDto<RecipeSearchResultDto> | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  // Re-runs the most recent search (either kind) for a given page
  let lastRequest: ((page: number) => Promise<void>) | null = null

  async function run(url: string, body: Record<string, unknown>) {
    loading.value = true
    error.value = null

    try {
      const response = await api.post<PagedResultDto<RecipeSearchResultDto>>(url, { ...body, pageSize: 20 })
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

  async function search(params: IngredientSearchParams, page = 1) {
    lastRequest = (p) => search(params, p)
    await run(endpoint, {
      mandatoryIngredientIds: params.mandatoryIds,
      optionalIngredientIds: params.optionalIds,
      optionalThreshold: params.threshold,
      page,
    })
  }

  async function searchCupboard(params: CupboardSearchParams, page = 1) {
    lastRequest = (p) => searchCupboard(params, p)
    await run(cupboardEndpoint, {
      ingredientIds: params.ingredientIds,
      includeMissing: params.includeMissing,
      page,
    })
  }

  async function loadPage(page: number) {
    if (lastRequest) await lastRequest(page)
  }

  function clear() {
    results.value = null
    error.value = null
    lastRequest = null
  }

  return { results, loading, error, search, searchCupboard, loadPage, clear }
}
