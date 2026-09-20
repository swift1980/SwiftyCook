<template>
  <transition-group name="card" tag="ul" class="grid">
    <!-- Show skeletons while loading -->
	<div v-if="loading" key="skeleton" class="card skeleton"></div>
	
	<!-- Show real data once loaded -->
    <div v-for="recipe in items" :key="recipe.id" class="card" @click="openRecipe(recipe)">
      {{ recipe.name }}
    </div>
  </transition-group>
  <!-- Recipe detail modal -->
  <RecipeCard v-if="selectedRecipe"
              :recipe="selectedRecipe"
              @close="selectedRecipe = null" />
</template>

<script setup lang="ts">
  import { ref, type PropType } from "vue"
  import RecipeCard from "./RecipeCard.vue"
  import type { RecipeCardItem } from '@/interfaces/ingredientSearch'

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

  const selectedRecipe = ref<RecipeCardItem | null>(null)

  function openRecipe(recipe: RecipeCardItem) {
    selectedRecipe.value = recipe
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
