<template>
  <main class="main">
    <!-- Search bar -->
    <div v-if="current != 'recipeform'" class="search-section">
      <div class="search-bar">
        <input v-model="nameQuery" type="text" placeholder="Search by recipe name" />
        <!-- Ingredient multi-tag input -->
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
        <button v-if="ingredientList.length"
                class="clear-ingredients"
                @click="ingredientList = []">
          Clear
        </button>
      </div>

      <!-- Filter controls -->
      <div class="filter-controls">
        <button v-if="current === 'recipes'"
                class="category-btn"
                @click="toggleCategories">
          Categories
        </button>
        <button class="advanced-btn" @click="toggleAdvancedSearch">
          {{ showAdvancedSearch ? 'Hide Advanced Search' : 'Advanced Search' }}
        </button>
      </div>

      <!-- Advanced search: an ingredient picker per IngredientType (1, 2, 3) -->
      <AdvancedSearch v-if="showAdvancedSearch" v-model="advancedIngredients" />

      <!-- Category modal -->
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

    <!-- Dynamic section content -->
    <div class="content">
      <Recipes v-if="current === 'recipes'"
	    :name-query="nameQuery"
       :ingredient-list="combinedIngredients"
        :selected-category-ids="selectedCategoryIds"
      />
      <ShoppingList v-if="current === 'shopping'" :query="query" />
      <MealPlanner v-if="current === 'planner'" :query="query" />
      <Cocktails
        v-if="current === 'cocktails'"
        :name-query="nameQuery"
        :ingredient-list="combinedIngredients"
        :selected-category-ids="[1]"
      />
	  <RecipeForm
	  v-if="current === 'recipeform'" />
    </div>
  </main>
</template>

<script setup lang="ts">
  import { ref, defineProps, onMounted, computed } from 'vue'
  import { useCategoryStore } from '@/stores/categoryStore'
  import { storeToRefs } from 'pinia'
  import Recipes from '@/views/Recipes.vue'
  import ShoppingList from '@/views/ShoppingList.vue'
  import MealPlanner from '@/views/MealPlanner.vue'
  import Cocktails from '@/views/Cocktails.vue'
  import RecipeForm from '@/views/RecipeForm.vue'
  import AdvancedSearch from '@/components/AdvancedSearch.vue'

  const props = defineProps({
    current: String
  })

  // Local reactive state
  const nameQuery = ref('')
  const query = ref('')

  // Advanced search
  const showAdvancedSearch = ref(false)
  const advancedIngredients = ref<string[]>([])
  const toggleAdvancedSearch = () => {
    showAdvancedSearch.value = !showAdvancedSearch.value
  }

  //Free-text tags + advanced search, deduplicated
  const combinedIngredients = computed(() => [...new Set([...ingredientList.value, ...advancedIngredients.value])])


    // Multi-ingredient state
    const ingredientInput = ref('')
    const ingredientList = ref<string[]>([])

    const addIngredient = () => {
      const value = ingredientInput.value.trim().replace(/,$/, '')
      if (value && !ingredientList.value.includes(value)) {
        ingredientList.value.push(value)
      }
      ingredientInput.value = ''
    }

    const removeIngredient = (index: number) => {
      ingredientList.value.splice(index, 1)
    }

    const handleBackspace = () => {
      if (ingredientInput.value === '' && ingredientList.value.length) {
        ingredientList.value.pop()
      }
    }
    const toggleCategories = () => {
      showCategories.value = !showCategories.value
    }

    // Category filter state
    const showCategories = ref(false)
    const selectedCategoryIds = ref<number[]>([])

    // Load categories from Pinia store
    const categoryStore = useCategoryStore()
    const { categories } = storeToRefs(categoryStore);

    onMounted(() => {
      categoryStore.fetchActive() // assumes your store has this action
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
      }

      .tag-remove:hover {
        color: #a00;
      }

      .clear-ingredients {
        font-size: 0.8rem;
        padding: 4px 8px;
        border: 1px solid #ccc;
        border-radius: 4px;
        background: none;
        cursor: pointer;
        color: #666;
      }

      .clear-ingredients:hover {
        background: #f5f5f5;
      }

      .ingredient-input-wrapper input {
        border: none;
        outline: none;
        flex: 1;
        min-width: 180px;
        font-size: 0.9rem;
        background: transparent;
      }

      .category-modal {
        background: #fff;
        padding: 1rem 1.5rem;
        border-radius: 0.5rem;
        min-width: 300px;
        max-height: 80vh;
        overflow-y: auto;
        box-shadow: 0 2px 10px rgba(0,0,0,0.2);

        h3 {
          margin-bottom: 0.5rem;
        }

        .category-option {
          margin: 0.25rem 0;
        }

        .close-btn {
          margin-top: 1rem;
          padding: 0.5rem 1rem;
          border-radius: 0.5rem;
          border: 1px solid #ccc;
          background: #f9f9f9;
          cursor: pointer;
        }
      }

      .content {
        flex: 1;
        overflow-y: auto;
        padding: 1rem;
      }
    }
</style>
