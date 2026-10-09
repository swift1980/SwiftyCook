<template>
  <transition-group name="card" tag="ul" class="grid">
    <!-- Show skeletons while loading -->
	<div v-if="loading" key="skeleton" class="card skeleton"></div>
	
	<!-- Show real data once loaded -->
    <div v-for="recipe in items" :key="recipe.id" class="card" @click="openRecipe(recipe)">
      {{ recipe.name }}
      <span v-if="recipe.missingCount" class="missing-badge">
        Missing {{ recipe.missingCount }}
      </span>
    </div>
  </transition-group>
  <!-- Recipe detail modal -->
  <RecipeCard v-if="selectedRecipe"
              :recipe="selectedRecipe"
              @close="selectedRecipe = null" />
  <p v-else-if="detailError" class="detail-error" role="alert">{{ detailError }}</p>
</template>

<script setup lang="ts">
  import { ref, type PropType } from "vue"
  import RecipeCard from "./RecipeCard.vue"
  import api from '@/services/api'
  import { getErrorMessage } from '@/utils/errors'
  import type { RecipeCardItem } from '@/interfaces/ingredientSearch'
  import type { RecipeDto } from '@/interfaces/recipe'

  defineProps({
    items: {
      type: Array as PropType<RecipeCardItem[]>,
      required: true
    },
	loading: {
	  type: Boolean,
	  default: false
	}
  });

  const selectedRecipe = ref<RecipeDto | null>(null)
  const detailError = ref<string | null>(null)

  async function openRecipe(recipe: RecipeCardItem) {
    detailError.value = null

    try {
      const response = await api.get<RecipeDto>(`/recipe/${recipe.id}`)
      selectedRecipe.value = response.data
    } catch (err: unknown) {
      detailError.value = getErrorMessage(err, 'Failed to load recipe details.')
    }
  }
</script>

<style lang="scss" scoped>
  @use "@/styles/index.scss" as *;

  .grid {
    display: grid;
    gap: 1rem;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  }

  .card {
    @include card;
  }

  .detail-error {
    color: #a00;
  }

  .missing-badge {
    display: block;
    font-size: 0.75rem;
    color: #a60;
  }

  /* --- Card animation --- */
  .card-enter-active, .card-leave-active {
    transition: all 0.3s ease;
  }

  .card-enter-from {
    opacity: 0;
    transform: translateY(20px);
  }

  .card-enter-to {
    opacity: 1;
    transform: translateY(0);
  }

  .card-leave-from {
    opacity: 1;
    transform: translateY(0);
  }

  .card-leave-to {
    opacity: 0;
    transform: translateY(20px);
  }
</style>
