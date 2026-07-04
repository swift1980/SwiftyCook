<template>
  <CardGrid :items="filtered" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import CardGrid from '@/components/shared/CardGrid.vue';

const props = defineProps({ query: String });

const shoppingItems = ['Milk', 'Eggs', 'Bread', 'Tomatoes', 'Cheese'];

const filtered = computed(() =>
  shoppingItems.filter(i =>
    i.toLowerCase().includes(props.query?.toLowerCase() || '')
  )
);
</script>
