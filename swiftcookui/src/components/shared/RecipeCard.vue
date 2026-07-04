<template>
  <div class="overlay" @click.self="$emit('close')">
    <div class="modal">
      <button class="close" @click="$emit('close')">✕</button>
      
      <h2>{{ recipe.name }}</h2>
      <p>{{ recipe.description }}</p>
      
      <h3>Ingredients</h3>
      <ul>
        <li v-for="ing in recipe.ingredients" :key="ing.ingredientId">
          {{ ing.amount }} {{ ing.unit.abbreviation }} {{ ing.ingredientName }}
        </li>
      </ul>

      <h3>Instructions</h3>
      <ol>
        <li v-for="step in recipe.instructions" :key="step.position">
          {{ step.step }}
        </li>
      </ol>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  recipe: {
    type: Object,
    required: true
  }
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
</style>
