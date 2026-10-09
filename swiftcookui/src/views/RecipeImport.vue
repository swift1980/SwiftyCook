<template>
  <div class="recipe-import">
    <h2>Import Recipes</h2>
    <p class="hint">
      Choose a JSON file in the
      <a href="https://github.com/swift1980/SwiftyCook/blob/main/docs/recipe-import-format.md" target="_blank" rel="noopener noreferrer">import format</a>
      (max 1 MB, 50 recipes). Nothing is saved until you confirm.
    </p>

    <input type="file" accept=".json,application/json" aria-label="Recipe file" @change="onFile" />

    <div v-if="store.error" class="error" role="alert">{{ store.error }}</div>

    <template v-if="store.result">
      <p v-if="store.result.dryRun" class="summary">Preview — {{ readyCount }} of {{ store.result.recipes.length }} recipe(s) ready to import.</p>
      <p v-else class="summary done">Import finished — {{ importedCount }} imported.</p>

      <section v-for="item in store.result.recipes" :key="item.index" class="recipe" :class="item.status">
        <header>
          <h3>{{ item.name || `Recipe ${item.index + 1}` }}</h3>
          <span class="badge" :class="item.status">{{ statusLabel(item.status) }}</span>
        </header>

        <ul v-if="item.errors.length" class="errors">
          <li v-for="(e, i) in item.errors" :key="i">{{ e }}</li>
        </ul>
        <ul v-if="item.warnings.length" class="warnings">
          <li v-for="(w, i) in item.warnings" :key="i">{{ w }}</li>
        </ul>

        <template v-if="item.ingredients.length && item.status !== 'invalid'">
          <div v-for="(line, i) in item.ingredients" :key="i" class="line">
            <span class="line-text">
              <template v-if="line.amount != null">{{ line.amount }} </template>{{ line.unit }} {{ line.name }}
            </span>

            <span v-if="line.resolution === 'exact'" class="ok">✓ {{ line.ingredientName }}</span>

            <span v-else class="decision">
              <label>
                Use existing
                <select
                  :aria-label="`Existing ingredient for ${line.name}`"
                  :value="existingChoice(line.name)"
                  @change="store.mapIngredient(line.name, toNumber(($event.target as HTMLSelectElement).value))"
                >
                  <option value="">—</option>
                  <option v-for="ing in ingredients" :key="ing.id" :value="ing.id">{{ ing.name }}</option>
                </select>
              </label>
              <label>
                or create in
                <select
                  :aria-label="`Category for new ingredient ${line.name}`"
                  :value="createChoice(line.name)"
                  @change="store.createIngredient(line.name, toNumber(($event.target as HTMLSelectElement).value))"
                >
                  <option value="">—</option>
                  <option v-for="cat in creatableCategories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
                </select>
              </label>
              <small v-if="line.resolution === 'near' && line.suggestedIngredientName">Did you mean “{{ line.suggestedIngredientName }}”?</small>
            </span>

            <span v-if="line.unit && line.unitResolution === 'unresolved'" class="decision">
              <label>
                Unit “{{ line.unit }}”
                <select
                  :aria-label="`Unit for ${line.unit}`"
                  :value="unitChoice(line.unit)"
                  @change="store.mapUnit(line.unit, toNumber(($event.target as HTMLSelectElement).value))"
                >
                  <option value="">—</option>
                  <option v-for="u in units" :key="u.id" :value="u.id">{{ u.name }}</option>
                </select>
              </label>
            </span>
          </div>
        </template>

        <p v-if="hasNew(item)" class="new-items">
          Will create:
          <template v-if="item.newCategories.length">categories {{ item.newCategories.join(', ') }}; </template>
          <template v-if="item.newTags.length">tags {{ item.newTags.join(', ') }}; </template>
          <template v-if="item.newTools.length">tools {{ item.newTools.join(', ') }}</template>
        </p>

        <div v-if="store.result.dryRun" class="choices">
          <label v-if="item.status === 'duplicate' || isChecked('importDuplicates', item.index)">
            <input type="checkbox" :checked="isChecked('importDuplicates', item.index)" @change="store.toggle('importDuplicates', item.index, ($event.target as HTMLInputElement).checked)" />
            Import anyway
          </label>
          <label>
            <input type="checkbox" :checked="isChecked('skip', item.index)" @change="store.toggle('skip', item.index, ($event.target as HTMLInputElement).checked)" />
            Skip this recipe
          </label>
        </div>
      </section>

      <div v-if="store.result.dryRun" class="actions">
        <button type="button" :disabled="store.loading || !store.dirty" @click="store.preview()">Update preview</button>
        <button type="button" class="confirm" :disabled="store.loading || store.dirty || !store.importable" @click="store.runImport()">
          Import {{ readyCount }} recipe(s)
        </button>
      </div>
      <div v-else class="actions">
        <router-link to="/recipes">View recipes</router-link>
        <button type="button" @click="store.reset()">Import another file</button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useRecipeImportStore, decisionKey } from '@/stores/recipeImportStore'
import { useIngredientStore } from '@/stores/ingredientStore'
import { useUnitStore } from '@/stores/unitStore'
import { useIngredientCategoryStore } from '@/stores/ingredientCategoryStore'
import type { ImportStatus, RecipeImportItem } from '@/interfaces/recipeImport'

const store = useRecipeImportStore()
const { ingredients } = storeToRefs(useIngredientStore())
const { units } = storeToRefs(useUnitStore())
const { categories } = storeToRefs(useIngredientCategoryStore())

const ingredientStore = useIngredientStore()
const unitStore = useUnitStore()
const categoryStore = useIngredientCategoryStore()

onMounted(async () => {
  store.reset()
  await Promise.all([ingredientStore.fetchAll(), unitStore.fetchAll(), categoryStore.fetchAll()])
})

// Only categories with a catch-all type can receive a new ingredient.
const creatableCategories = computed(() => categories.value.filter((c) => c.fallbackTypeId != null))

const readyCount = computed(() => store.result?.recipes.filter((r) => r.status === 'ready').length ?? 0)
const importedCount = computed(() => store.result?.recipes.filter((r) => r.status === 'imported').length ?? 0)

const labels: Record<ImportStatus, string> = {
  invalid: 'Invalid',
  skipped: 'Skipped',
  needsDecision: 'Needs your decision',
  duplicate: 'Name already exists',
  ready: 'Ready',
  imported: 'Imported',
  failed: 'Failed',
}
const statusLabel = (s: ImportStatus) => labels[s] ?? s

const hasNew = (item: RecipeImportItem) =>
  item.newCategories.length + item.newTags.length + item.newTools.length > 0

const toNumber = (v: string) => (v === '' ? null : Number(v))
const existingChoice = (name: string) => store.decisions.ingredients[decisionKey(name)]?.ingredientId ?? ''
const createChoice = (name: string) => store.decisions.ingredients[decisionKey(name)]?.createCategoryId ?? ''
const unitChoice = (name: string) => store.decisions.units[decisionKey(name)] ?? ''
const isChecked = (list: 'skip' | 'importDuplicates', index: number) => store.decisions[list].includes(index)

async function onFile(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  const text = file.size > 1024 * 1024 ? '' : await file.text()
  await store.loadFile(file.name, file.size, text)
}
</script>

<style lang="scss" scoped>
.recipe-import {
  max-width: 900px;
  margin: 0 auto;
  padding: 1rem;
}
.hint { color: #6b7280; }
.error { color: #b91c1c; margin: 0.75rem 0; }
.summary { margin: 1rem 0; font-weight: 600; }
.recipe {
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  margin-bottom: 1rem;

  header { display: flex; justify-content: space-between; align-items: center; }
  h3 { margin: 0; }
}
.badge {
  font-size: 0.8rem;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: #e5e7eb;

  &.ready, &.imported { background: #bbf7d0; }
  &.needsDecision, &.duplicate { background: #fde68a; }
  &.invalid, &.failed { background: #fecaca; }
}
.errors { color: #b91c1c; }
.warnings { color: #92400e; }
.line {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 1rem;
  align-items: center;
  padding: 0.25rem 0;
  border-bottom: 1px solid #f3f4f6;
}
.line-text { min-width: 12rem; }
.ok { color: #15803d; }
.decision { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
.choices { display: flex; gap: 1rem; margin-top: 0.5rem; }
.actions { display: flex; gap: 1rem; margin-top: 1rem; }
.confirm { font-weight: 600; }
</style>
