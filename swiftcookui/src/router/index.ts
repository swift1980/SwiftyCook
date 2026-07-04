import { createRouter, createWebHistory } from 'vue-router';
import Recipes from '@/views/Recipes.vue';

const routes = [
  { path: '/', redirect: "/recipes" },
  { path: '/recipes', name: 'Recipes', component: Recipes }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

export default router;
