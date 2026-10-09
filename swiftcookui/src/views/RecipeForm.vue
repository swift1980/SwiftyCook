<template>
  <form class="recipe-form" @submit.prevent="submitForm">
    <h2>Create New Recipe</h2>

    <!-- Core recipe info -->
    <div class="form-group">
      <label>Recipe Name</label>
      <input v-model="form.name" required />
    </div>

    <div class="form-group">
      <label>Description</label>
      <textarea v-model="form.description"></textarea>
    </div>

    <div class="form-group">
      <label>Image URL</label>
      <input v-model="form.image" type="url" />
    </div>

    <div class="form-group">
      <label>Prep Time (minutes)</label>
      <input type="number" v-model.number="form.prepTime" />
    </div>

    <div class="form-group">
      <label>Cook Time (minutes)</label>
      <input type="number" v-model.number="form.cookTime" />
    </div>

    <div class="form-group">
      <label>Yield</label>
      <input type="number" v-model.number="form.yield" />
    </div>

    <!-- Categories -->
    <div class="form-group">
      <label>Categories</label>
      <div v-for="c in categories" :key="c.id">
        <input
          type="checkbox"
          :value="c.id"
          v-model="form.categoryIds"
        />
        {{ c.name }}
      </div>
    </div>

    <!-- Tags -->
    <div class="form-group">
      <label>Tags</label>
      <div v-for="t in tags" :key="t.id">
        <input
          type="checkbox"
          :value="t.id"
          v-model="form.tagIds"
        />
        {{ t.name }}
      </div>
    </div>

    <!-- Tools -->
    <div class="form-group">
      <label>Tools</label>
      <div v-for="tool in tools" :key="tool.id">
        <input
          type="checkbox"
          :value="tool.id"
          v-model="form.toolIds"
        />
        {{ tool.name }}
      </div>
    </div>

<!-- Ingredients -->
<div v-for="(ri, index) in form.ingredients" :key="index" class="ingredient-row">
  <!-- Select or type new ingredient -->
  <input
    v-model="ri.ingredientName"
    placeholder="Type ingredient name"
    list="ingredient-list"
  />
  <datalist id="ingredient-list">
    <option v-for="ing in ingredients" :key="ing.id" :value="ing.name" />
  </datalist>

  <span v-if="suggestedMatches[index]" class="ingredient-suggestion">
    Did you mean
    <button type="button" class="suggestion-btn" @click="useSuggestion(index)">
      "{{ suggestedMatches[index] }}"
    </button>?
  </span>

  <!-- Ticket 10: a brand-new ingredient (no existing exact match) needs a
       category so its auto-created IngredientType isn't NULL/invisible in
       Advanced Search; reuses each category's Ticket 6 FallbackTypeId. -->
  <select v-if="needsCategory[index]" v-model.number="ri.categoryId" required class="new-ingredient-category">
    <option disabled :value="null">New ingredient — select a category</option>
    <option v-for="category in rootCategories" :key="category.id" :value="category.id">
      {{ category.name }}
    </option>
  </select>

  <input type="number" v-model.number="ri.amount" placeholder="Amount" />

  <select v-model="ri.unitId" required>
    <option disabled value="">Select unit</option>
    <option v-for="unit in units" :key="unit.id" :value="unit.id">
      {{ unit.name }}
    </option>
  </select>

  <input type="text" v-model="ri.note" maxlength="255" placeholder="Note (e.g. finely chopped)" />

  <button type="button" @click="removeIngredient(index)">✕</button>
</div>
<button type="button" @click="addIngredient">+ Add Ingredient</button>

    <!-- Instructions -->
    <div class="form-group">
      <label>Instructions</label>
      <div
        v-for="(step, index) in form.instructions"
        :key="index"
        class="instruction-row"
      >
        <textarea v-model="step.step" placeholder="Step description" required />
        <button type="button" @click="removeInstruction(index)">✕</button>
      </div>
      <button type="button" @click="addInstruction">+ Add Step</button>
    </div>

    <!-- Submit -->
    <button type="submit" class="submit-btn">Save Recipe</button>
  </form>
</template>

<script setup lang="ts">
import { reactive, onMounted, computed } from 'vue'
import { storeToRefs } from 'pinia'
import { useRecipeStore } from '@/stores/recipeStore'
import { useCategoryStore } from '@/stores/categoryStore'
import { useTagStore } from '@/stores/tagStore'
import { useToolStore } from '@/stores/toolStore'
import { useIngredientStore } from '@/stores/ingredientStore'
import { useIngredientCategoryStore } from '@/stores/ingredientCategoryStore'
import { useUnitStore } from '@/stores/unitStore'
import { findClosestMatch } from '@/utils/similarity'
import type { RecipeCreateDto } from "../interfaces/recipe";

const recipeStore = useRecipeStore()
const categoryStore = useCategoryStore()
const tagStore = useTagStore()
const toolStore = useToolStore()
const ingredientStore = useIngredientStore()
const ingredientCategoryStore = useIngredientCategoryStore()
const unitStore = useUnitStore()
const { categories } = storeToRefs(categoryStore);
const { units } = storeToRefs(unitStore);
const { ingredients } = storeToRefs(ingredientStore);
const { tags } = storeToRefs(tagStore);
const { tools } = storeToRefs(toolStore);

// Ticket 10: root IngredientCategory list, so a brand-new ingredient typed
// directly into a recipe can be assigned a category (and thus a non-NULL
// TypeId via that category's Ticket 6 FallbackTypeId) instead of ending up
// invisible in Advanced Search.
const rootCategories = computed(() =>
  ingredientCategoryStore.categories.filter((c) => c.parentCategoryId == null)
)

type FormIngredient = 
{
  ingredientId: number | null
  ingredientName: string
  amount: number | null
  unitId: number | null
  position: number
  categoryId: number | null
  note: string
}


// reactive form
const form = reactive({
  name: '',
  description: '',
  image: '',
  prepTime: 0,
  cookTime: 0,
  yield: 1,
  categoryIds: [] as number[],
  tagIds: [] as number[],
  toolIds: [] as number[],
  ingredients: [
    { ingredientId: null, ingredientName: '', amount: null as number | null, unitId: null as number | null, position: 1, categoryId: null, note: '' } as FormIngredient
  ],
  instructions: [{ step: '', position: 1 }]
})

// UI actions
const addIngredient = () => {
  form.ingredients.push({
    ingredientId: null,
	ingredientName: '',
    amount: null,
    unitId: null,
    position: form.ingredients.length + 1,
    categoryId: null,
    note: ''
  } as FormIngredient)
}
const removeIngredient = (i: number) => form.ingredients.splice(i, 1)

// Ticket 7: "Did you mean '‹existing name›'?" per ingredient row, so a typo
// doesn't silently spawn a redundant new Ingredient at submit time.
const suggestedMatches = computed(() =>
  form.ingredients.map((ri) => {
    const typed = ri.ingredientName?.trim()
    if (!typed || ri.ingredientId != null) return null
    return findClosestMatch(typed, ingredients.value.map((i) => i.name))
  })
)

/** Accepts a fuzzy suggestion for a row, replacing the typed name with the existing ingredient's name (which will then resolve via exact match at submit time). */
const useSuggestion = (index: number) => {
  const suggestion = suggestedMatches.value[index]
  if (!suggestion) return
  form.ingredients[index].ingredientName = suggestion
}

// Ticket 10: rows with a typed name that doesn't exactly match an existing
// ingredient will be auto-created at submit time, so they need a category
// chosen up front (used to resolve a non-NULL fallback TypeId) — otherwise
// they end up invisible in every Advanced Search box.
const needsCategory = computed(() =>
  form.ingredients.map((ri) => {
    const typed = ri.ingredientName?.trim()
    if (!typed || ri.ingredientId != null) return false
    return !ingredients.value.some((i) => i.name.toLowerCase() === typed.toLowerCase())
  })
)

const addInstruction = () => {
  form.instructions.push({
    step: '',
    position: form.instructions.length + 1
  })
}
const removeInstruction = (i: number) => form.instructions.splice(i, 1)

const normalizePositions = () => {
  form.ingredients.forEach((ri, i) => (ri.position = i + 1));
  form.instructions.forEach((step, i) => (step.position = i + 1));
};

const submitForm = async () => {
  try {
    // Ticket 10: block save if any new ingredient is missing its required
    // category — otherwise it would be auto-created with TypeId = NULL and
    // become invisible in every Advanced Search box.
    const missingCategory = form.ingredients.some((ri, i) => needsCategory.value[i] && ri.categoryId == null)
    if (missingCategory) {
      alert('Please select a category for each new ingredient before saving.')
      return
    }

    for (const ri of form.ingredients) {
	  if (ri.ingredientId == null && ri.ingredientName) {
	    //Check if ingredient already exists
		const existing = ingredients.value.find(i => i.name.toLowerCase() === ri.ingredientName.toLowerCase())
		if (existing) {
		  ri.ingredientId = existing.id
		}
		else
		{
		  const category = ingredientCategoryStore.categories.find((c) => c.id === ri.categoryId)
		  const typeId = category?.fallbackTypeId ?? undefined
		  const newIng = await ingredientStore.createIngredient({ name: ri.ingredientName, pluralName: '', typeId })
		  ri.ingredientId = newIng.id
		  ingredients.value.push(newIng)
		}
	  }
	}
	
    normalizePositions();
    const dto = toDto()
    await recipeStore.createRecipe(dto);
    alert('Recipe created successfully!');
	resetForm();
  } catch (err) {
    console.error(err);
    alert('Error saving recipe');
  }
};

const resetForm = () => {
  form.name = '';
  form.description = '';
  form.image = '';
  form.prepTime = 0;
  form.cookTime = 0;
  form.yield = 1;
  form.categoryIds = [];
  form.tagIds = [];
  form.toolIds = [];
  form.ingredients = [{ ingredientId: null, ingredientName: '', amount: null, unitId: null, position: 1, categoryId: null, note: '' }];
  form.instructions = [{ step: '', position: 1 }];
};

const toDto = () => {
  return {
    name: form.name,
    description: form.description,
    image: form.image,
    prepTime: form.prepTime,
    cookTime: form.cookTime,
    yield: form.yield,
    categoryIds: form.categoryIds,
    tagIds: form.tagIds,
    toolIds: form.toolIds,
    ingredients: form.ingredients.map((ri, i) => ({
      ingredientId: Number(ri.ingredientId),
      amount: ri.amount ?? null,
      unitId: ri.unitId ? Number(ri.unitId) : 1,
      position: i + 1,
      note: ri.note.trim() || null
    })),
    instructions: form.instructions.map((step, i) => ({
      step: step.step,
      position: i + 1
    }))
  } as RecipeCreateDto
}

// Load dropdown data when form mounts
onMounted(async () => {
  await Promise.all([
    categoryStore.fetchAll(),
    tagStore.fetchAll(),
    toolStore.fetchAll(),
    ingredientStore.fetchAll(),
    ingredientCategoryStore.fetchAll(),
    unitStore.fetchAll()
  ])
})
</script>


<style scoped>
.recipe-form {
  max-width: 700px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.ingredient-row,
.instruction-row {
  display: flex;
  gap: 0.5rem;
  align-items: flex-start;
}

.ingredient-suggestion {
  font-size: 0.8rem;
  color: #555;
  align-self: center;
}

.suggestion-btn {
  background: none;
  border: none;
  padding: 0;
  color: #1a5fb4;
  text-decoration: underline;
  cursor: pointer;
  font-size: inherit;
}

.suggestion-btn:hover {
  opacity: 0.7;
}

button {
  cursor: pointer;
}

.submit-btn {
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  border: 1px solid #ccc;
  background: #f9f9f9;
}
</style>
