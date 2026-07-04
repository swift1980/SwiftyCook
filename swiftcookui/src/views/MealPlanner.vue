<template>
  <CardGrid :items="filtered" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import CardGrid from '@/components/shared/CardGrid.vue';

const props = defineProps({ query: String });

const meals = ['Breakfast: Oatmeal', 'Lunch: Salad', 'Dinner: Pasta', 'Snack: Fruit'];

const filtered = computed(() =>
  meals.filter(m =>
    m.toLowerCase().includes(props.query?.toLowerCase() || '')
  )
);
</script>
