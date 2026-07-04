<template>
  <div>
    <!-- Overlay for mobile -->
    <div
      v-if="open"
      class="overlay"
      @click="$emit('update:open', false)"
    ></div>

    <aside :class="['sidebar', open ? 'open' : '']">
      <h1>SwiftCook</h1>
      <nav>
        <button @click="$emit('update:current', 'recipes')" :class="{active: current==='recipes'}">Recipes</button>
        <button @click="$emit('update:current', 'cocktails')" :class="{active: current==='cocktails'}">Cocktails</button>
        <button @click="$emit('update:current', 'shopping')" :class="{active: current==='shopping'}">Shopping List</button>
        <button @click="$emit('update:current', 'planner')" :class="{active: current==='planner'}">Meal Planner</button>
		<button @click="$emit('update:current', 'recipeform')" :class="{active: current==='recipeform'}">Add Recipe</button>
      </nav>
    </aside>
  </div>
</template>

<script setup>
defineProps({
  current: String,
  open: Boolean
});
defineEmits(['update:current', 'update:open']);
</script>

<style lang="scss" scoped>
.sidebar {
  width: 16rem;
  background: #111827;
  color: white;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  position: fixed;
  top: 0;
  left: -16rem; // hidden by default
  height: 100vh;
  z-index: 50;
  transition: left 0.3s ease;

  &.open {
    left: 0;
  }

  h1 {
    margin-bottom: 1rem;
  }

  button {
    display: block;
    background: transparent;
    border: none;
    color: white;
    padding: 0.5rem;
    text-align: left;
    cursor: pointer;
    margin-bottom: 0.25rem;

    &.active {
      background: #374151;
    }

    &:hover {
      background: #374151;
    }
  }

  @media (min-width: 640px) {
    left: 0;
    position: static;
    width: 16rem;
  }
}

/* Overlay behind sidebar on mobile */
.overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  z-index: 40;
  transition: opacity 0.3s ease;
}
</style>
