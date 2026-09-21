<template>
  <main class="main">
    <div v-if="route.name !== 'RecipeForm'" class="search-section">
      <div class="search-bar">
        <input v-model="nameQuery" type="text" placeholder="Search by recipe name" />
      </div>

      <div class="filter-controls">
        <button v-if="route.name === 'Recipes'" class="category-btn" @click="toggleCategories">
          Categories
        </button>
        <button v-if="route.name === 'Recipes'" class="advanced-btn" @click="toggleAdvancedSearch">
          {{ showAdvancedSearch ? 'Hide Ingredient Search' : 'Search by Ingredient' }}
        </button>
      </div>

      <AdvancedSearch v-if="route.name === 'Recipes' && showAdvancedSearch"
                      @search="onAdvancedSearch"
                      @clear="onAdvancedClear" />

      <!-- Pagination controls — visible only when server results are active -->
      <div v-if="route.name === 'Recipes' && ingredientSearch.results.value" class="pagination-controls">
        <button :disabled="ingredientSearch.results.value.page <= 1"
                @click="ingredientSearch.loadPage(ingredientSearch.results.value.page - 1)">
          ‹ Prev
        </button>
        <span>Page {{ ingredientSearch.results.value.page }} of {{ ingredientSearch.results.value.totalPages }}</span>
        <button :disabled="ingredientSearch.results.value.page >= ingredientSearch.results.value.totalPages"
                @click="ingredientSearch.loadPage(ingredientSearch.results.value.page + 1)">
          Next ›
        </button>
      </div>

      <div v-if="showCategories" class="modal-overlay" @click.self="showCategories = false">
        <div class="category-modal">
          <h3>Select Categories</h3>
          <div v-for="cat in categories" :key="cat.id" class="category-option">
            <label>
              <input type="checkbox" :value="cat.id" v-model="selectedCategoryIds" />
              {{ cat.name }}
            </label>
          </div>
          <button class="close-btn" @click="showCategories = false">Close</button>
        </div>
      </div>
    </div>

    <div class="content">
      <router-view v-slot="{ Component }">
        <component :is="Component"
                   v-bind="routeProps"
                   v-on="route.name === 'Recipes' ? { retry: retrySearch } : {}" />
      </router-view>
    </div>
  </main>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted, watch } from 'vue'
  import { useRoute } from 'vue-router'
  import { useCategoryStore } from '@/stores/categoryStore'
  import { storeToRefs } from 'pinia'
  import { useIngredientSearch } from '@/composables/useIngredientSearch'
  import { useNameSearch } from '@/composables/useNameSearch'
  import AdvancedSearch from '@/components/AdvancedSearch.vue'
  import type { IngredientSearchParams } from '@/interfaces/ingredientSearch'

  const route = useRoute()

  const nameQuery = ref('')
  const showAdvancedSearch = ref(false)
  const showCategories = ref(false)
  const selectedCategoryIds = ref<number[]>([])
  const lastParams = ref<IngredientSearchParams | null>(null)

  const ingredientSearch = useIngredientSearch()
  const nameSearch = useNameSearch()

  const categoryStore = useCategoryStore()
  const { categories } = storeToRefs(categoryStore)

  onMounted(() => {
    categoryStore.fetchActive()
  })

  let nameSearchTimeout: ReturnType<typeof setTimeout> | undefined

  watch(nameQuery, (q) => {
    if (nameSearchTimeout) clearTimeout(nameSearchTimeout)
    nameSearchTimeout = setTimeout(() => nameSearch.search(q), 300)
  })

  function toggleAdvancedSearch() {
    showAdvancedSearch.value = !showAdvancedSearch.value
    if (!showAdvancedSearch.value) onAdvancedClear()
  }

  function toggleCategories() {
    showCategories.value = !showCategories.value
  }

  function onAdvancedSearch(params: IngredientSearchParams) {
    lastParams.value = params
    ingredientSearch.search(params, 1)
  }

  function onAdvancedClear() {
    lastParams.value = null
    ingredientSearch.clear()
  }

  function retrySearch() {
    if (lastParams.value) ingredientSearch.search(lastParams.value, 1)
  }

  const recipesProps = computed(() => ({
    nameQuery: nameQuery.value,
    selectedCategoryIds: selectedCategoryIds.value,
    searchResults: ingredientSearch.results.value,
    searchError: ingredientSearch.error.value,
    searchLoading: ingredientSearch.loading.value,
    nameSearchResults: nameSearch.results.value,
    nameSearchError: nameSearch.error.value,
    nameSearchLoading: nameSearch.loading.value,
  }))

  const cocktailsProps = computed(() => ({
    nameQuery: nameQuery.value,
  }))

  const routeProps = computed(() => {
    if (route.name === 'Recipes') return recipesProps.value
    if (route.name === 'Cocktails') return cocktailsProps.value
    return {}
  })
</script>

<style lang="scss" scoped>
  @use "@/styles/index.scss" as *;

  .main {
    flex: 1;
    display: flex;
    flex-direction: column;
    margin-left: 0;
    transition: margin-left 0.3s ease;

    .search-bar {
      display: flex;
      gap: 1rem;
      padding: 1rem;

      input {
        width: 100%;
        padding: 0.5rem;
        border-radius: 0.5rem;
        border: 1px solid #ccc;
        flex: 1;
      }
    }

    .search-section {
      border-bottom: 1px solid #ddd;
    }

    .filter-controls {
      display: flex;
      gap: 0.75rem;
      padding: 0.5rem 1rem;

      .category-btn,
      .advanced-btn {
        @include button;
      }
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.5rem 1rem;
      font-size: 0.9rem;

      button {
        @include button;
        padding: 4px 10px;

        &:disabled {
          opacity: 0.4;
          cursor: not-allowed;
        }
      }
    }

    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.3);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }
  }
</style>
