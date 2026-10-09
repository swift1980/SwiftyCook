<template>
  <div class="overlay" @click.self="$emit('close')">
    <div class="modal">
      <button class="close" @click="$emit('close')">✕</button>
      
      <h2>{{ recipe.name }}</h2>
      <p>{{ recipe.description }}</p>
      
      <div class="plan">
        <button type="button" class="plan-btn" @click="planning = true">Add to meal planner</button>
        <span v-if="planned" class="planned" role="status">{{ planned }}</span>
      </div>

      <h3>Ingredients</h3>
      <ul>
        <li v-for="ing in recipe.ingredients" :key="ing.ingredientId">
          <template v-if="ing.amount != null">{{ [ing.amount, ing.unit?.abbreviation ?? ing.unit?.name].filter(Boolean).join(' ') + ' ' }}</template>{{ ing.ingredientName }}<template v-if="ing.note">, {{ ing.note }}</template>
        </li>
      </ul>

      <h3>Instructions</h3>
      <ol>
        <li v-for="step in recipe.instructions" :key="step.position">
          {{ step.step }}
        </li>
      </ol>

      <p v-if="recipe.sourceUrl || recipe.sourceTitle || recipe.sourceAuthor" class="source">
        Source:
        <a v-if="safeSourceUrl" :href="safeSourceUrl" target="_blank" rel="noopener noreferrer">{{ recipe.sourceTitle || recipe.sourceUrl }}</a>
        <template v-else>{{ recipe.sourceTitle || recipe.sourceUrl }}</template>
        <template v-if="recipe.sourceAuthor"> by {{ recipe.sourceAuthor }}</template>
      </p>
    </div>
    <MealPlanAddDialog v-if="planning" :recipe="recipe" @close="planning = false" @added="onAdded" />
  </div>
</template>

<script setup lang="ts">
import { computed, ref, type PropType } from 'vue'
import MealPlanAddDialog from '@/components/MealPlanAddDialog.vue'
import type { MealType } from '@/interfaces/mealPlan'
import { addedMessage } from '@/utils/mealType'
import { parseIsoDate } from '@/utils/dates'
import type { RecipeDto } from '@/interfaces/recipe'

const props = defineProps({
  recipe: {
    type: Object as PropType<RecipeDto>,
    required: true
  }
})

const planning = ref(false)
const planned = ref('')

function onAdded(date: string, mealType: MealType) {
  const weekday = parseIsoDate(date).toLocaleDateString('en-GB', { weekday: 'long' })
  planned.value = addedMessage(date, mealType, weekday)
}

// Only http(s) links are rendered as anchors.
const safeSourceUrl = computed(() => {
  const url = props.recipe.sourceUrl
  return url && /^https?:\/\//i.test(url) ? url : null
})
</script>

<style scoped>
.overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.6);
  display: flex;
  align-items: center;
  justify-content: center;
}
.modal {
  background: #fff;
  border-radius: 1rem;
  padding: 2rem;
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  position: relative;
}
.close {
  position: absolute;
  top: 1rem;
  right: 1rem;
  background: transparent;
  border: none;
  font-size: 1.2rem;
  cursor: pointer;
}
.plan { display: flex; align-items: center; gap: 0.75rem; margin: 0.5rem 0; }
.planned { color: #15803d; }
.source {
  margin-top: 1rem;
  font-size: 0.9rem;
  color: #6b7280;
}
</style>
