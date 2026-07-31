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

  <input type="number" v-model.number="ri.amount" placeholder="Amount" />

  <select v-model="ri.unitId" required>
    <option disabled value="">Select unit</option>
    <option v-for="unit in units" :key="unit.id" :value="unit.id">
      {{ unit.name }}
    </option>
  </select>

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
import { reactive, ref, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useRecipeStore } from '@/stores/recipeStore'
import { useCategoryStore } from '@/stores/categoryStore'
import { useTagStore } from '@/stores/tagStore'
import { useToolStore } from '@/stores/toolStore'
import { useIngredientStore } from '@/stores/ingredientStore'
import { useUnitStore } from '@/stores/unitStore'
import type { RecipeCreateDto } from "../interfaces/recipe";

const recipeStore = useRecipeStore()
const categoryStore = useCategoryStore()
const tagStore = useTagStore()
const toolStore = useToolStore()
const ingredientStore = useIngredientStore()
const unitStore = useUnitStore()
const { categories } = storeToRefs(categoryStore);
const { units } = storeToRefs(unitStore);
const { ingredients } = storeToRefs(ingredientStore);
const { tags } = storeToRefs(tagStore);
const { tools } = storeToRefs(toolStore);

type FormIngredient = 
{
  ingredientId: number | null
  ingredientName: string
  amount: number | null
  unitId: number | null
  position: number
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
    { ingredientId: null, ingredientName: '', amount: null as number | null, unitId: null as number | null, position: 1 } as FormIngredient
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
    position: form.ingredients.length + 1
  } as FormIngredient)
}
const removeIngredient = (i: number) => form.ingredients.splice(i, 1)

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
    for (const ri of form.ingredients) {
	  if (ri.ingredientId == null && ri.ingredientName) {
	    //Check if ingredient already exists
		const existing = ingredients.value.find(i => i.name.toLowerCase() === ri.ingredientName.toLowerCase())
		if (existing) {
		  ri.ingredientId = existing.id
		}
		else
		{
		  const newIng = await ingredientStore.createIngredient({ name: ri.ingredientName, pluralName: '' })
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
  form.ingredients = [{ ingredientId: null, ingredientName: '', amount: null, unitId: null, position: 1 }];
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
      amount: ri.amount ?? 0,
      unitId: ri.unitId ? Number(ri.unitId) : 1,
      position: i + 1
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
