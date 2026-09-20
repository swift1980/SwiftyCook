<template>
  <div class="flex h-screen">
    <!-- Sidebar -->
    <aside class="w-64 bg-gray-900 text-white p-4">
      <h1 class="text-xl font-bold mb-6">Recipe System</h1>
      <nav class="space-y-2">
        <button
          v-for="item in menuItems"
          :key="item"
          class="w-full text-left px-3 py-2 rounded hover:bg-gray-700"
          :class="{ 'bg-gray-700': activeSection === item }"
          @click="activeSection = item"
        >
          {{ item }}
        </button>
      </nav>
    </aside>

    <!-- Main Content -->
    <main class="flex-1 flex flex-col">
      <!-- Search Bar -->
      <div class="p-4 border-b bg-gray-100">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search..."
          class="w-full p-2 rounded border focus:ring focus:ring-blue-300"
        />
      </div>

      <!-- Results -->
      <div class="flex-1 overflow-y-auto p-4">
        <h2 class="text-lg font-semibold mb-4">
          {{ activeSection }} Results
        </h2>
        <ul class="space-y-2">
          <li
            v-for="(item, index) in filteredResults"
            :key="index"
            class="p-3 bg-white rounded shadow hover:bg-gray-50 cursor-pointer"
          >
            {{ item }}
          </li>
        </ul>
        <p v-if="filteredResults.length === 0" class="text-gray-500">
          No results found.
        </p>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";

const menuItems = ["Recipes", "Shopping List", "Meal Planner"];
const activeSection = ref("Recipes");
const searchQuery = ref("");

// Dummy data for demonstration
const data = {
  Recipes: ["Pasta", "Salad", "Pizza", "Soup"],
  "Shopping List": ["Tomatoes", "Cheese", "Bread", "Olive Oil"],
  "Meal Planner": ["Monday - Pasta", "Tuesday - Salad", "Wednesday - Pizza"],
};

const filteredResults = computed(() => {
  const items = data[activeSection.value] || [];
  return items.filter((item) =>
    item.toLowerCase().includes(searchQuery.value.toLowerCase())
  );
});
</script>

<style>
/* Optional: Customize scrollbar for results */
::-webkit-scrollbar {
  width: 6px;
}
::-webkit-scrollbar-thumb {
  background: #888;
  border-radius: 4px;
}
::-webkit-scrollbar-thumb:hover {
  background: #555;
}
</style>
