<template>
  <div class="cupboard">
    <h2>Cupboard</h2>

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

      <button type="submit" :disabled="!form.ingredientId || !form.unitId">Add to cupboard</button>
    </form>

    <div v-if="cupboardStore.error" class="error">{{ cupboardStore.error }}</div>

    <ul v-if="cupboardStore.items.length" class="cupboard-list">
      <li v-for="item in cupboardStore.items" :key="item.ingredientId" class="cupboard-item">
        <span class="name">{{ item.ingredientName }}</span>
        <span v-if="item.amount != null" class="amount">{{ item.amount }} {{ item.unitName }}</span>
        <button type="button" class="remove-btn" @click="onRemove(item.ingredientId)">✕</button>
      </li>
    </ul>
    <div v-else-if="!cupboardStore.loading" class="empty-state">Your cupboard is empty.</div>
  </div>
</template>

<script setup lang="ts">
  import { reactive, onMounted } from 'vue'
  import { useCupboardStore } from '@/stores/cupboardStore'
  import { useIngredientStore } from '@/stores/ingredientStore'
  import { useUnitStore } from '@/stores/unitStore'
  import { storeToRefs } from 'pinia'

  const cupboardStore = useCupboardStore()
  const ingredientStore = useIngredientStore()
  const unitStore = useUnitStore()

  defineOptions({ name: 'CupboardView' })

  const { ingredients } = storeToRefs(ingredientStore)
  const { units } = storeToRefs(unitStore)

  const form = reactive({
    ingredientId: null as number | null,
    unitId: null as number | null,
    amount: null as number | null,
  })

  onMounted(async () => {
    await Promise.all([
      cupboardStore.fetchAll(),
      ingredientStore.fetchAll(),
      unitStore.fetchAll(),
    ])
  })

  async function onAdd() {
    if (!form.ingredientId || !form.unitId) return
    await cupboardStore.addItem({
      ingredientId: form.ingredientId,
      unitId: form.unitId,
      amount: form.amount ?? undefined,
    })
    form.ingredientId = null
    form.unitId = null
    form.amount = null
  }

  async function onRemove(ingredientId: number) {
    await cupboardStore.removeItem(ingredientId)
  }
</script>

<style lang="scss" scoped>
.cupboard {
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

.cupboard-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.cupboard-item {
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
