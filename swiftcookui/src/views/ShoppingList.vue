<template>
  <div class="shopping-list">
    <h2>Shopping List</h2>

    <form class="add-form" @submit.prevent="onAdd">
      <select v-model.number="form.ingredientId" required>
        <option disabled value="">Select ingredient</option>
        <option v-for="ingredient in ingredients" :key="ingredient.id" :value="ingredient.id">
          {{ ingredient.name }}
        </option>
      </select>

      <input type="number" min="0" step="any" v-model.number="form.amount" placeholder="Amount (optional)" />

      <select v-model.number="form.unitId" required>
        <option disabled value="">Select unit</option>
        <option v-for="unit in units" :key="unit.id" :value="unit.id">
          {{ unit.name }}
        </option>
      </select>

      <button type="submit" :disabled="!form.ingredientId || !form.unitId">Add to shopping list</button>
    </form>

    <div v-if="shoppingListStore.error" class="error">{{ shoppingListStore.error }}</div>

    <ul v-if="shoppingListStore.items.length" class="shopping-list-items">
      <li v-for="item in shoppingListStore.items" :key="item.id" class="shopping-list-item">
        <span class="name">{{ item.ingredientName }}</span>
        <span v-if="item.amount != null" class="amount">{{ item.amount }} {{ item.unitName }}</span>
        <button type="button" class="remove-btn" @click="onRemove(item.id)">✕</button>
      </li>
    </ul>
    <div v-else-if="!shoppingListStore.loading" class="empty-state">Your shopping list is empty.</div>
  </div>
</template>

<script setup lang="ts">
  import { reactive, onMounted } from 'vue'
  import { useShoppingListStore } from '@/stores/shoppingListStore'
  import { useIngredientStore } from '@/stores/ingredientStore'
  import { useUnitStore } from '@/stores/unitStore'
  import { storeToRefs } from 'pinia'

  const shoppingListStore = useShoppingListStore()
  const ingredientStore = useIngredientStore()
  const unitStore = useUnitStore()

  const { ingredients } = storeToRefs(ingredientStore)
  const { units } = storeToRefs(unitStore)

  const form = reactive({
    ingredientId: null as number | null,
    unitId: null as number | null,
    amount: null as number | null,
  })

  onMounted(async () => {
    await Promise.all([
      shoppingListStore.fetchAll(),
      ingredientStore.fetchAll(),
      unitStore.fetchAll(),
    ])
  })

  async function onAdd() {
    if (!form.ingredientId || !form.unitId) return
    await shoppingListStore.addItem({
      ingredientId: form.ingredientId,
      unitId: form.unitId,
      amount: form.amount ?? undefined,
    })
    form.ingredientId = null
    form.unitId = null
    form.amount = null
  }

  async function onRemove(id: number) {
    await shoppingListStore.removeItem(id)
  }
</script>

<style lang="scss" scoped>
.shopping-list {
  max-width: 700px;
  margin: 0 auto;
  padding: 1rem;
}

.add-form {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.add-form select,
.add-form input {
  padding: 0.5rem;
  border-radius: 0.5rem;
  border: 1px solid #ccc;
}

.shopping-list-items {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.shopping-list-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid #eee;
  border-radius: 0.5rem;

  .name {
    flex: 1;
    font-weight: 600;
  }

  .amount {
    color: #666;
  }

  .remove-btn {
    background: none;
    border: none;
    cursor: pointer;
    color: #a00;
  }
}

.error {
  color: #a00;
  margin-bottom: 1rem;
}

.empty-state {
  color: #666;
}
</style>
