import { createRouter, createWebHistory } from 'vue-router';
import Recipes from '@/views/Recipes.vue';
import RecipeForm from '@/views/RecipeForm.vue';
import ShoppingList from '@/views/ShoppingList.vue';
import Cocktails from '@/views/Cocktails.vue';
import Cupboard from '@/views/Cupboard.vue';

const routes = [
  { path: '/', redirect: "/recipes" },
  { path: '/recipes', name: 'Recipes', component: Recipes },
  { path: '/recipes/new', name: 'RecipeForm', component: RecipeForm },
  { path: '/shopping-list', name: 'ShoppingList', component: ShoppingList },
  { path: '/cocktails', name: 'Cocktails', component: Cocktails },
  { path: '/cupboard', name: 'Cupboard', component: Cupboard }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

export default router;
