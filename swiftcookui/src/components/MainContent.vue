<template>
  <main class="main">
    <div v-if="current != 'recipeform'" class="search-section">
      <div class="search-bar">
        <input v-model="nameQuery" type="text" placeholder="Search by recipe name" />
        <div class="ingredient-input-wrapper">
          <div class="ingredient-tags">
            <span v-for="(ing, index) in ingredientList"
                  :key="index"
                  class="ingredient-tag">
              {{ ing }}
              <button @click="removeIngredient(index)" class="tag-remove">x</button>
            </span>
          </div>
          <input v-model="ingredientInput"
                 type="text"
                 placeholder="Add ingredient and press Enter"
                 @keydown.enter.prevent="addIngredient"
                 @keydown.backspace="handleBackspace" />
        </div>
        <button v-if="ingredientList.length" class="clear-ingredients" @click="ingredientList = []">
          Clear
        </button>
      </div>

      <div class="filter-controls">
        <button v-if="current === 'recipes'" class="category-btn" @click="toggleCategories">
          Categories
        </button>
        <button class="advanced-btn" @click="toggleAdvancedSearch">
          {{ showAdvancedSearch ? 'Hide Advanced Search' : 'Advanced Search' }}
        </button>
      </div>

      <AdvancedSearch v-if="showAdvancedSearch"
                      @search="onAdvancedSearch"
                      @clear="onAdvancedClear" />

      <!-- Pagination controls — visible only when server results are active -->
      <div v-if="ingredientSearch.results.value" class="pagination-controls">
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
      <Recipes v-if="current === 'recipes'"
               :name-query="nameQuery"
               :ingredient-list="ingredientList"
               :selected-category-ids="selectedCategoryIds"
               :search-results="ingredientSearch.results.value"
               :search-error="ingredientSearch.error.value"
               :search-loading="ingredientSearch.loading.value"
               :name-search-results="nameSearch.results.value"
               :name-search-error="nameSearch.error.value"
               :name-search-loading="nameSearch.loading.value"
               @retry="retrySearch" />
      <ShoppingList v-if="current === 'shopping'" :query="query" />
      <MealPlanner v-if="current === 'planner'" :query="query" />
      <Cocktails v-if="current === 'cocktails'"
                 :name-query="nameQuery"
                 :ingredient-list="ingredientList"
                 :selected-category-ids="[1]" />
      <RecipeForm v-if="current === 'recipeform'" />
    </div>
  </main>
</template>

<script setup lang="ts">
  import { ref, onMounted, watch } from 'vue'
  import { useCategoryStore } from '@/stores/categoryStore'
  import { storeToRefs } from 'pinia'
  import { useIngredientSearch } from '@/composables/useIngredientSearch'
  import { useNameSearch } from '@/composables/useNameSearch'
  import Recipes from '@/views/Recipes.vue'
  import ShoppingList from '@/views/ShoppingList.vue'
  import MealPlanner from '@/views/MealPlanner.vue'
  import Cocktails from '@/views/Cocktails.vue'
  import RecipeForm from '@/views/RecipeForm.vue'
  import AdvancedSearch from '@/components/AdvancedSearch.vue'
  import type { IngredientSearchParams } from '@/interfaces/ingredientSearch'

  defineProps({ current: String })

  const nameQuery = ref('')
  const query = ref('')
  const ingredientInput = ref('')
  const ingredientList = ref<string[]>([])
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

  function addIngredient() {
    const value = ingredientInput.value.trim().replace(/,$/, '')
    if (value && !ingredientList.value.includes(value)) ingredientList.value.push(value)
    ingredientInput.value = ''
  }

  function removeIngredient(index: number) {
    ingredientList.value.splice(index, 1)
  }

  function handleBackspace() {
    if (ingredientInput.value === '' && ingredientList.value.length) ingredientList.value.pop()
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

    .ingredient-input-wrapper {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 6px;
      border: 1px solid #ccc;
      border-radius: 6px;
      padding: 4px 8px;
      min-height: 40px;
      flex: 1;
    }

    .ingredient-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }

    .ingredient-tag {
      display: flex;
      align-items: center;
      gap: 4px;
      background: #e8f4e8;
      color: #2d6a2d;
      border-radius: 4px;
      padding: 2px 8px;
      font-size: 0.875rem;
    }

    .tag-remove {
      background: none;
      border: none;
      cursor: pointer;
      color: #2d6a2d;
      font-size: 1rem;
      padding: 0;
      line-height: 1;

      &:hover {
        color: #a00;
      }
    }

    .clear-ingredients {
      font-size: 0.8rem;
      padding: 4px 8px;
      border: 1px solid #ccc;
      border-radius: 4px;
      background: none;
      cursor: pointer;
      color: #666;

      &:hover {
        background: #f5f5f5;
      }
    }

    .ingredient-input-wrapper input {
      border: none;
      outline: none;
      flex: 1;
      min-width: 180px;
      font-size: 0.9rem;
      background: transparent;
    }
  }
</style>
