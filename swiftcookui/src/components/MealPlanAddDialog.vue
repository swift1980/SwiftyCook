<template>
  <div class="overlay" @click.self="$emit('close')">
    <div class="dialog" role="dialog" aria-label="Add to meal plan">
      <button class="close" type="button" @click="$emit('close')">✕</button>
      <h3>Add to {{ mealType }}</h3>
      <p class="when">{{ date }}</p>

      <input v-model="query" class="search" type="search" placeholder="Search recipes by name" />

      <ul class="results">
        <li v-for="r in matches" :key="r.id">
          <button
            type="button"
            :class="{ selected: r.id === selectedId }"
            @click="select(r)"
          >{{ r.name }}</button>
        </li>
      </ul>
      <div v-if="!recipeStore.loading && !cocktailStore.loading && !matches.length" class="empty">No matching recipes.</div>

      <label class="servings">
        Servings
        <input v-model.number="servings" type="number" min="1" max="1000" />
      </label>

      <div v-if="error" class="error">{{ error }}</div>

      <button class="add" type="button" :disabled="!selectedId || !servings || servings < 1" @click="onAdd">
        Add
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRecipeStore } from '@/stores/recipeStore'
import { useCocktailStore } from '@/stores/cocktailStore'
import { useMealPlanStore } from '@/stores/mealPlanStore'
import { COCKTAIL_CATEGORY_ID, type MealType } from '@/interfaces/mealPlan'
import type { RecipeDto } from '@/interfaces/recipe'
import { getErrorMessage } from '@/utils/errors'

const props = defineProps<{ date: string; mealType: MealType }>()
const emit = defineEmits<{ close: []; added: [] }>()

const recipeStore = useRecipeStore()
const cocktailStore = useCocktailStore()
const mealPlanStore = useMealPlanStore()

const query = ref('')
const selectedId = ref<number | null>(null)
const servings = ref(1)
const error = ref<string | null>(null)

// The recipe list excludes cocktails, which have their own endpoint.
const isCocktailRow = props.mealType === 'Cocktail'

onMounted(() => {
  if (isCocktailRow) {
    if (!cocktailStore.cocktails.length) cocktailStore.fetchCocktailAll()
  } else if (!recipeStore.recipes.length) {
    recipeStore.fetchAllRecipes()
  }
})

// The Cocktail row only offers cocktails; every other row excludes them.
const matches = computed(() => {
  const q = query.value.trim().toLowerCase()
  const source = isCocktailRow ? cocktailStore.cocktails : recipeStore.recipes
  return source
    .filter(r => (r.categoryIds ?? []).includes(COCKTAIL_CATEGORY_ID) === isCocktailRow)
    .filter(r => !q || r.name.toLowerCase().includes(q))
})

function select(recipe: RecipeDto) {
  selectedId.value = recipe.id
  servings.value = Math.max(recipe.yield || 1, 1)
}

async function onAdd() {
  if (!selectedId.value) return
  error.value = null
  try {
    await mealPlanStore.addEntry({
      date: props.date,
      mealType: props.mealType,
      recipeId: selectedId.value,
      servings: servings.value,
    })
    emit('added')
    emit('close')
  } catch (err: unknown) {
    error.value = getErrorMessage(err, 'Failed to add to the meal plan')
  }
}
</script>

<style scoped>
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 60;
}
.dialog {
  position: relative;
  background: #fff;
  border-radius: 1rem;
  padding: 1.5rem;
  width: min(28rem, 92vw);
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.close {
  position: absolute;
  top: 0.5rem;
  right: 0.75rem;
  background: none;
  border: none;
  cursor: pointer;
}
.when { margin: 0; color: #666; }
.search, .servings input { padding: 0.5rem; border: 1px solid #ccc; border-radius: 0.5rem; }
.results { list-style: none; padding: 0; margin: 0; overflow-y: auto; max-height: 14rem; }
.results button {
  width: 100%;
  text-align: left;
  padding: 0.4rem 0.6rem;
  background: none;
  border: none;
  cursor: pointer;
}
.results button:hover, .results button.selected { background: #e5e7eb; }
.servings { display: flex; gap: 0.5rem; align-items: center; }
.servings input { width: 5rem; }
.empty { color: #666; }
.error { color: #a00; }
.add { padding: 0.5rem; border-radius: 0.5rem; cursor: pointer; }
</style>
