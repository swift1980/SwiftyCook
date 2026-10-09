<template>
  <div class="ingredient-manager">
    <h2>Ingredients</h2>

    <div class="tabs" role="tablist">
      <button type="button" role="tab" :class="{ active: tab === 'ingredients' }" @click="tab = 'ingredients'">
        Ingredients
      </button>
      <button type="button" role="tab" :class="{ active: tab === 'types' }" @click="tab = 'types'">
        Types
      </button>
    </div>

    <div v-if="message" class="error" role="alert">{{ message }}</div>

    <!-- Ingredients tab -->
    <section v-if="tab === 'ingredients'">
      <form class="add-form" @submit.prevent="onAddIngredient">
        <input v-model="newIngredient.name" placeholder="Name" required />
        <input v-model="newIngredient.pluralName" placeholder="Plural name" />
        <select v-model.number="newIngredient.typeId" required>
          <option :value="null" disabled>Type</option>
          <option v-for="t in types" :key="t.id" :value="t.id">{{ t.name }}</option>
        </select>
        <label class="check"><input type="checkbox" v-model="newIngredient.isStaple" /> Staple</label>
        <button type="submit" :disabled="!newIngredient.name.trim() || newIngredient.typeId == null">Add</button>
      </form>

      <div class="filters">
        <input v-model="search" placeholder="Search by name" aria-label="Search ingredients" />
        <select v-model.number="filterCategoryId" aria-label="Filter by category">
          <option :value="null">All categories</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <select v-model.number="filterTypeId" aria-label="Filter by type">
          <option :value="null">All types</option>
          <option v-for="t in filterTypeOptions" :key="t.id" :value="t.id">{{ t.name }}</option>
        </select>
      </div>

      <table v-if="filteredIngredients.length" class="grid">
        <thead>
          <tr><th>Name</th><th>Plural</th><th>Type</th><th>Staple</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="i in filteredIngredients" :key="i.id">
            <template v-if="editingIngredientId === i.id">
              <td><input v-model="ingredientDraft.name" aria-label="Edit name" /></td>
              <td><input v-model="ingredientDraft.pluralName" aria-label="Edit plural name" /></td>
              <td>
                <select v-model.number="ingredientDraft.typeId" aria-label="Edit type">
                  <option v-for="t in types" :key="t.id" :value="t.id">{{ t.name }}</option>
                </select>
              </td>
              <td><input type="checkbox" v-model="ingredientDraft.isStaple" aria-label="Edit staple" /></td>
              <td class="actions">
                <button type="button" @click="saveIngredient(i.id)">Save</button>
                <button type="button" @click="editingIngredientId = null">Cancel</button>
              </td>
            </template>
            <template v-else>
              <td>{{ i.name }}</td>
              <td>{{ i.pluralName }}</td>
              <td>{{ i.typeName }}</td>
              <td>{{ i.isStaple ? '✓' : '' }}</td>
              <td class="actions">
                <button type="button" @click="startEditIngredient(i)">Edit</button>
                <button type="button" class="danger" @click="onDeleteIngredient(i.id)">Delete</button>
              </td>
            </template>
          </tr>
        </tbody>
      </table>
      <div v-else class="empty-state">No ingredients match.</div>
    </section>

    <!-- Types tab -->
    <section v-else>
      <form class="add-form" @submit.prevent="onAddType">
        <input v-model="newType.name" placeholder="Type name" required />
        <select v-model.number="newType.categoryId" required>
          <option :value="null" disabled>Category</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <button type="submit" :disabled="!newType.name.trim() || newType.categoryId == null">Add</button>
      </form>

      <table v-if="types.length" class="grid">
        <thead>
          <tr><th>Name</th><th>Category</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="t in types" :key="t.id">
            <template v-if="editingTypeId === t.id">
              <td><input v-model="typeDraft.name" aria-label="Edit type name" /></td>
              <td>
                <select v-model.number="typeDraft.categoryId" aria-label="Edit type category">
                  <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
                </select>
              </td>
              <td class="actions">
                <button type="button" @click="saveType(t.id)">Save</button>
                <button type="button" @click="editingTypeId = null">Cancel</button>
              </td>
            </template>
            <template v-else>
              <td>{{ t.name }}</td>
              <td>{{ categoryName(t.categoryId) }}</td>
              <td class="actions">
                <button type="button" @click="startEditType(t)">Edit</button>
                <button type="button" class="danger" @click="onDeleteType(t.id)">Delete</button>
              </td>
            </template>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted, reactive, ref } from 'vue'
  import { useIngredientStore } from '@/stores/ingredientStore'
  import { useIngredientTypeStore } from '@/stores/ingredientTypeStore'
  import { useIngredientCategoryStore } from '@/stores/ingredientCategoryStore'
  import type { IngredientDto } from '@/interfaces/ingredient'
  import type { IngredientTypeDto } from '@/interfaces/ingredientType'

  defineOptions({ name: 'IngredientManagerView' })

  const ingredientStore = useIngredientStore()
  const typeStore = useIngredientTypeStore()
  const categoryStore = useIngredientCategoryStore()

  const tab = ref<'ingredients' | 'types'>('ingredients')
  const message = ref<string | null>(null)

  const types = computed(() => typeStore.ingredientTypes)
  const categories = computed(() => categoryStore.categories)

  onMounted(async () => {
    await Promise.all([ingredientStore.fetchAll(), typeStore.fetchAll(), categoryStore.fetchAll()])
  })

  function categoryName(id: number) {
    return categories.value.find((c) => c.id === id)?.name ?? ''
  }

  // Runs a store action; store errors (including API 409 usage messages) are shown inline.
  async function run(action: () => Promise<unknown>, errorSource: () => string | null) {
    message.value = null
    try {
      await action()
      return true
    } catch {
      message.value = errorSource()
      return false
    }
  }

  // --- Ingredients ---
  const search = ref('')
  const filterCategoryId = ref<number | null>(null)
  const filterTypeId = ref<number | null>(null)

  const filterTypeOptions = computed(() =>
    filterCategoryId.value == null
      ? types.value
      : types.value.filter((t) => t.categoryId === filterCategoryId.value),
  )

  const filteredIngredients = computed(() => {
    const q = search.value.trim().toLowerCase()
    const categoryOf = new Map(types.value.map((t) => [t.id, t.categoryId]))
    return ingredientStore.ingredients
      .filter((i) => !q || i.name.toLowerCase().includes(q))
      .filter((i) => filterTypeId.value == null || i.typeId === filterTypeId.value)
      .filter(
        (i) =>
          filterCategoryId.value == null ||
          (i.typeId != null && categoryOf.get(i.typeId) === filterCategoryId.value),
      )
      .sort((a, b) => a.name.localeCompare(b.name))
  })

  const newIngredient = reactive({
    name: '',
    pluralName: '',
    typeId: null as number | null,
    isStaple: false,
  })

  async function onAddIngredient() {
    if (newIngredient.typeId == null) return
    const ok = await run(
      () =>
        ingredientStore.createIngredient({
          name: newIngredient.name.trim(),
          pluralName: newIngredient.pluralName.trim(),
          typeId: newIngredient.typeId ?? undefined,
          isStaple: newIngredient.isStaple,
        }),
      () => ingredientStore.error,
    )
    if (ok) {
      newIngredient.name = ''
      newIngredient.pluralName = ''
      newIngredient.isStaple = false
    }
  }

  const editingIngredientId = ref<number | null>(null)
  const ingredientDraft = reactive({ name: '', pluralName: '', typeId: null as number | null, isStaple: false })

  function startEditIngredient(i: IngredientDto) {
    editingIngredientId.value = i.id
    ingredientDraft.name = i.name
    ingredientDraft.pluralName = i.pluralName ?? ''
    ingredientDraft.typeId = i.typeId ?? null
    ingredientDraft.isStaple = !!i.isStaple
  }

  async function saveIngredient(id: number) {
    const ok = await run(
      () =>
        ingredientStore.updateIngredient(id, {
          name: ingredientDraft.name.trim(),
          pluralName: ingredientDraft.pluralName.trim(),
          typeId: ingredientDraft.typeId ?? undefined,
          isStaple: ingredientDraft.isStaple,
        }),
      () => ingredientStore.error,
    )
    if (ok) editingIngredientId.value = null
  }

  async function onDeleteIngredient(id: number) {
    await run(() => ingredientStore.deleteIngredient(id), () => ingredientStore.error)
  }

  // --- Types ---
  const newType = reactive({ name: '', categoryId: null as number | null })

  async function onAddType() {
    if (newType.categoryId == null) return
    const ok = await run(
      () =>
        typeStore.createIngredientType({ name: newType.name.trim(), categoryId: newType.categoryId ?? undefined }),
      () => typeStore.error,
    )
    if (ok) newType.name = ''
  }

  const editingTypeId = ref<number | null>(null)
  const typeDraft = reactive({ name: '', categoryId: null as number | null })

  function startEditType(t: IngredientTypeDto) {
    editingTypeId.value = t.id
    typeDraft.name = t.name
    typeDraft.categoryId = t.categoryId
  }

  async function saveType(id: number) {
    const ok = await run(
      () =>
        typeStore.updateIngredientType(id, {
          name: typeDraft.name.trim(),
          categoryId: typeDraft.categoryId ?? undefined,
        }),
      () => typeStore.error,
    )
    if (ok) {
      editingTypeId.value = null
      // Ingredient rows show typeName, which may have changed with a rename.
      await ingredientStore.fetchAll()
    }
  }

  async function onDeleteType(id: number) {
    await run(() => typeStore.deleteIngredientType(id), () => typeStore.error)
  }
</script>

<style lang="scss" scoped>
.ingredient-manager {
  max-width: 900px;
  margin: 0 auto;
  padding: 1rem;
}

.tabs {
  display: flex;
  gap: 0.25rem;
  margin-bottom: 1rem;

  button {
    padding: 0.5rem 1rem;
    border: 1px solid #ccc;
    border-radius: 0.5rem;
    background: white;
    cursor: pointer;

    &.active {
      background: #111827;
      color: white;
    }
  }
}

.add-form,
.filters {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;

  input,
  select {
    padding: 0.5rem;
    border-radius: 0.5rem;
    border: 1px solid #ccc;
  }

  .check {
    display: flex;
    align-items: center;
    gap: 0.25rem;
  }
}

.grid {
  width: 100%;
  border-collapse: collapse;

  th,
  td {
    text-align: left;
    padding: 0.4rem 0.5rem;
    border-bottom: 1px solid #eee;
  }

  td input,
  td select {
    width: 100%;
    padding: 0.3rem;
  }

  .actions {
    white-space: nowrap;

    button {
      margin-left: 0.25rem;
    }

    .danger {
      color: #b91c1c;
    }
  }
}

.error {
  color: #b91c1c;
  margin-bottom: 0.75rem;
}

.empty-state {
  color: #666;
}
</style>
