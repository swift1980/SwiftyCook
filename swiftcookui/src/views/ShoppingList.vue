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
        <span v-if="item.source" class="source">{{ item.source }}</span>
        <button type="button" class="remove-btn" @click="onRemove(item.id)">✕</button>
      </li>
    </ul>
    <div v-else-if="!shoppingListStore.loading" class="empty-state">Your shopping list is empty.</div>

    <button v-if="shoppingListStore.items.length" type="button" class="purchased-btn" @click="confirming = true">Purchased</button>

    <div v-if="confirming" class="overlay" @click.self="confirming = false">
      <div class="dialog" role="dialog" aria-label="Confirm purchase">
        <h3>Move to your cupboard?</h3>
        <ul class="confirm-items">
          <li v-for="item in shoppingListStore.items" :key="item.id">
            {{ item.ingredientName }}<template v-if="item.amount != null"> – {{ item.amount }} {{ item.unitName }}</template>
          </li>
        </ul>
        <div class="dialog-actions">
          <button type="button" class="cancel" @click="confirming = false">Cancel</button>
          <button type="button" class="confirm" :disabled="purchasing" @click="onPurchase">Confirm</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { reactive, ref, onMounted } from 'vue'
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

  const confirming = ref(false)
  const purchasing = ref(false)

  async function onPurchase() {
    purchasing.value = true
    try {
      await shoppingListStore.purchase(shoppingListStore.items.map(i => i.id))
    } catch {
      // the store exposes the error
    } finally {
      purchasing.value = false
      confirming.value = false
    }
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

  .source {
    color: #92400e;
    font-size: 0.8rem;
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

.purchased-btn {
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  cursor: pointer;
}

.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 60;
}

.dialog {
  background: #fff;
  border-radius: 1rem;
  padding: 1.5rem;
  width: min(26rem, 92vw);
  max-height: 90vh;
  overflow-y: auto;
}

.confirm-items {
  padding-left: 1.25rem;
}

.dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
