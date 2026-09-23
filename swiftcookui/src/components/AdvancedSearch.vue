<template>
  <section class="advanced-search">
    <h3 class="advanced-search__title">Advanced Search</h3>

    <div class="advanced-search__grid">
      <div v-for="box in categoryBoxes" :key="box.categoryId" class="advanced-search__field">
        <label :for="`ingredient-category-${box.categoryId}`">{{ box.categoryName }}</label>

        <div class="advanced-search__box">
          <div v-if="selectedByCategory[box.categoryId]?.length" class="ingredient-tags">
            <span v-for="item in selectedByCategory[box.categoryId]"
                  :key="item.id"
                  class="ingredient-tag"
                  :class="item.mandatory ? 'ingredient-tag--mandatory' : 'ingredient-tag--optional'">
              <button type="button"
                      class="tag-mode"
                      :title="item.mandatory ? 'Mandatory — click to make optional' : 'Optional — click to make mandatory'"
                      @click="toggleMandatory(item.id)">
                {{ item.mandatory ? 'M' : 'O' }}
              </button>
              {{ item.name }}
              <span v-if="item.typeName" class="ingredient-tag-type">({{ item.typeName }})</span>
              <button type="button" class="tag-remove" @click="removeIngredient(item.id)">x</button>
            </span>
          </div>

          <input :id="`ingredient-category-${box.categoryId}`"
                 v-model="inputs[box.categoryId]"
                 type="text"
                 :list="`ingredient-options-${box.categoryId}`"
                 :placeholder="`Search ${box.categoryName.toLowerCase()}`"
                 @keydown.enter.prevent="addIngredient(box.categoryId)" />
          <datalist :id="`ingredient-options-${box.categoryId}`">
            <option v-for="name in box.options" :key="name" :value="name" />
          </datalist>
        </div>

        <span v-if="suggestedMatch[box.categoryId]" class="suggestion">
          Did you mean
          <button type="button" class="suggestion-btn" @click="useSuggestion(box.categoryId)">
            "{{ suggestedMatch[box.categoryId] }}"
          </button>?
        </span>

        <button v-if="pendingCreateName[box.categoryId]"
                type="button"
                class="add-new-btn"
                @click="confirmCreate(box.categoryId)">
          + Add "{{ pendingCreateName[box.categoryId] }}" as new ingredient
        </button>
      </div>
    </div>

    <!-- Threshold control — only shown when there are optional ingredients -->
    <div v-if="optionalIngredients.length" class="advanced-search__threshold">
      <label for="optional-threshold">
        Match at least
        <strong>{{ threshold }}</strong>
        of {{ optionalIngredients.length }} optional ingredient{{ optionalIngredients.length !== 1 ? 's' : '' }}
      </label>
      <input id="optional-threshold"
             v-model.number="threshold"
             type="range"
             :min="0"
             :max="optionalIngredients.length"
             class="threshold-slider" />
      <span v-if="thresholdError" class="threshold-error">{{ thresholdError }}</span>
    </div>

    <span v-if="ingredientStore.error" class="create-error">{{ ingredientStore.error }}</span>

    <div class="advanced-search__actions">
      <button type="button"
              class="cupboard-btn"
              :disabled="cupboardStore.loading"
              @click="useCupboard">
        Use my cupboard
      </button>
      <button type="button"
              class="search-btn"
              :disabled="!canSearch"
              :class="{ 'search-btn--dirty': isDirty }"
              @click="submitSearch">
        Search{{ isDirty ? ' ●' : '' }}
      </button>
      <button type="button" class="clear-btn" @click="clearAll">Clear</button>
    </div>
  </section>
</template>

<script setup lang="ts">
  import { reactive, ref, computed, watch, onMounted } from 'vue'
  import { useIngredientStore } from '@/stores/ingredientStore'
  import { useIngredientTypeStore } from '@/stores/ingredientTypeStore'
  import { useIngredientCategoryStore } from '@/stores/ingredientCategoryStore'
  import { useCupboardStore } from '@/stores/cupboardStore'
  import { findClosestMatch } from '@/utils/similarity'
  import type { IngredientSearchParams } from '@/interfaces/ingredientSearch'

  const emit = defineEmits<{
    (e: 'search', params: IngredientSearchParams): void
    (e: 'clear'): void
  }>()

  const ingredientStore = useIngredientStore()
  const ingredientTypeStore = useIngredientTypeStore()
  const ingredientCategoryStore = useIngredientCategoryStore()
  const cupboardStore = useCupboardStore()

  interface SelectedIngredient {
    id: number
    name: string
    mandatory: boolean
    // Specific IngredientType label, shown on the tag for clarity even though
    // the box itself is grouped by the broader IngredientCategory (Ticket 8).
    typeName?: string
  }

  // Keyed by categoryId: current text input value
  const inputs = reactive<Record<number, string>>({})

  // All selected ingredients flat, keyed by ingredient id for O(1) lookup
  const selectedMap = reactive<Record<number, SelectedIngredient>>({})

  const threshold = ref(0)
  const isDirty = ref(false)
  const lastSubmittedKey = ref('')

  // typeId -> categoryId, so ingredients (which only carry typeId/typeName) can
  // be grouped under their root IngredientCategory.
  const typeCategoryMap = computed(() => {
    const map: Record<number, number> = {}
    for (const type of ingredientTypeStore.ingredientTypes) {
      map[type.id] = type.categoryId
    }
    return map
  })

  // One box per root IngredientCategory (fixed, small count) instead of one per
  // IngredientType (unbounded, user-creatable) — see BACKLOG.md Ticket 8.
  const categoryBoxes = computed(() =>
    ingredientCategoryStore.categories
      .filter((c) => c.parentCategoryId == null)
      .map((category) => ({
        categoryId: category.id,
        categoryName: category.name,
        options: ingredientStore.ingredients
          .filter((i) => i.typeId != null && typeCategoryMap.value[i.typeId] === category.id)
          .map((i) => i.name),
      }))
  )

  // Initialise inputs when categoryBoxes resolves
  watch(categoryBoxes, (boxes) => {
    boxes.forEach((b) => {
      if (!(b.categoryId in inputs)) inputs[b.categoryId] = ''
    })
  }, { immediate: true })

  const selectedByCategory = computed(() => {
    const map: Record<number, SelectedIngredient[]> = {}
    for (const item of Object.values(selectedMap)) {
      const typeId = ingredientStore.ingredients.find((i) => i.id === item.id)?.typeId
      const categoryId = typeId != null ? typeCategoryMap.value[typeId] : undefined
      if (categoryId == null) continue
      if (!map[categoryId]) map[categoryId] = []
      map[categoryId].push(item)
    }
    return map
  })

  /** Finds an existing ingredient by exact case-insensitive name, scoped to a category (spanning all its child types). */
  function findExistingMatch(categoryId: number, name: string) {
    const lower = name.toLowerCase()
    return ingredientStore.ingredients.find((i) => {
      if (i.typeId == null || typeCategoryMap.value[i.typeId] !== categoryId) return false
      return i.name.toLowerCase() === lower
    })
  }

  // Ticket 6: when a typed name doesn't match any existing ingredient in its
  // category, surface an explicit "+ Add as new ingredient" confirm affordance
  // instead of silently discarding it.
  const pendingCreateName = computed(() => {
    const map: Record<number, string> = {}
    for (const box of categoryBoxes.value) {
      const value = inputs[box.categoryId]?.trim()
      if (!value) continue
      if (!findExistingMatch(box.categoryId, value)) {
        map[box.categoryId] = value
      }
    }
    return map
  })

  // Ticket 7: "Did you mean '‹existing name›'?" — a near-duplicate check
  // (exact-match already ruled out by pendingCreateName) so a typo doesn't
  // spawn a redundant new Ingredient when an existing one was intended.
  const suggestedMatch = computed(() => {
    const map: Record<number, string> = {}
    for (const [categoryId, typed] of Object.entries(pendingCreateName.value)) {
      const box = categoryBoxes.value.find((b) => b.categoryId === Number(categoryId))
      if (!box) continue
      const match = findClosestMatch(typed, box.options)
      if (match) map[Number(categoryId)] = match
    }
    return map
  })

  /** Accepts a fuzzy suggestion in place of creating a new ingredient. */
  function useSuggestion(categoryId: number) {
    const suggestion = suggestedMatch.value[categoryId]
    if (!suggestion) return
    inputs[categoryId] = suggestion
    addIngredient(categoryId)
  }

  const optionalIngredients = computed(() =>
    Object.values(selectedMap).filter((i) => !i.mandatory)
  )

  const thresholdError = computed(() => {
    const max = optionalIngredients.value.length
    if (threshold.value < 0 || threshold.value > max) {
      return `Threshold must be between 0 and ${max}.`
    }
    return null
  })

  const canSearch = computed(() => {
    const hasIngredients = Object.keys(selectedMap).length > 0
    return hasIngredients && thresholdError.value === null
  })

  // Mark dirty whenever selection or threshold changes after a search
  watch([() => ({ ...selectedMap }), threshold], () => {
    isDirty.value = true
  })

  function addIngredient(categoryId: number) {
    const value = inputs[categoryId]?.trim()
    if (!value) return
    const match = findExistingMatch(categoryId, value)
    if (match) {
      if (!(match.id in selectedMap)) {
        selectedMap[match.id] = { id: match.id, name: match.name, mandatory: false, typeName: match.typeName }
      }
      inputs[categoryId] = ''
    }
    // No match: leave the input as-is. pendingCreateName reactively surfaces
    // a "+ Add as new ingredient" confirm affordance instead of silently
    // discarding the typed text (Ticket 6).
  }

  /** Explicit confirm step (Ticket 6): creates the typed name as a real Ingredient, typed under the category's fixed fallback IngredientType, and selects it. */
  async function confirmCreate(categoryId: number) {
    const value = inputs[categoryId]?.trim()
    if (!value) return
    const fallbackTypeId = ingredientCategoryStore.categories.find((c) => c.id === categoryId)?.fallbackTypeId
    if (fallbackTypeId == null) {
      console.error(`IngredientCategory ${categoryId} has no FallbackTypeId configured; cannot create ingredient.`)
      return
    }
    try {
      const created = await ingredientStore.createIngredient({ name: value, pluralName: '', typeId: fallbackTypeId })
      selectedMap[created.id] = { id: created.id, name: created.name, mandatory: false, typeName: created.typeName }
      inputs[categoryId] = ''
    } catch {
      // ingredientStore.error is already set by createIngredient and rendered in the template.
    }
  }

  function removeIngredient(id: number) {
    delete selectedMap[id]
    // Clamp threshold if optional pool shrinks below it
    const max = optionalIngredients.value.length
    if (threshold.value > max) threshold.value = max
  }

  function toggleMandatory(id: number) {
    if (selectedMap[id]) {
      selectedMap[id].mandatory = !selectedMap[id].mandatory
      // Clamp threshold after toggling
      const max = optionalIngredients.value.length
      if (threshold.value > max) threshold.value = max
    }
  }

  /** Prefills the optional ingredient list from the user's cupboard contents (Amount is ignored - presence-only). */
  async function useCupboard() {
    if (!ingredientStore.ingredients.length) await ingredientStore.fetchAll()
    if (!cupboardStore.items.length) await cupboardStore.fetchAll()

    for (const item of cupboardStore.items) {
      if (item.ingredientId in selectedMap) continue
      // Only add ingredients we can resolve a type for, so they render in a category box
      const known = ingredientStore.ingredients.find((i) => i.id === item.ingredientId)
      if (!known) continue
      selectedMap[item.ingredientId] = { id: item.ingredientId, name: item.ingredientName, mandatory: false, typeName: known.typeName }
    }
  }

  function submitSearch() {
    if (!canSearch.value) return
    const params: IngredientSearchParams = {
      mandatoryIds: Object.values(selectedMap).filter((i) => i.mandatory).map((i) => i.id),
      optionalIds: optionalIngredients.value.map((i) => i.id),
      threshold: threshold.value,
    }
    isDirty.value = false
    lastSubmittedKey.value = JSON.stringify(params)
    emit('search', params)
  }

  function clearAll() {
    Object.keys(selectedMap).forEach((k) => delete selectedMap[Number(k)])
    threshold.value = 0
    isDirty.value = false
    emit('clear')
  }

  onMounted(() => {
    if (!ingredientStore.ingredients.length) ingredientStore.fetchAll()
    if (!ingredientTypeStore.ingredientTypes.length) ingredientTypeStore.fetchAll()
    if (!ingredientCategoryStore.categories.length) ingredientCategoryStore.fetchAll()
  })
</script>

<style lang="scss" scoped>
  @use "@/styles/index.scss" as *;

  .advanced-search {
    padding: 1rem;
    border-top: 1px solid #ddd;

    &__title {
      margin: 0 0 0.75rem;
      font-size: 1rem;
    }

    &__grid {
      display: flex;
      gap: 1rem;
      flex-wrap: wrap;
    }

    &__field {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      flex: 1;
      min-width: 200px;

      label {
        font-size: 0.85rem;
        color: #555;
      }
    }

    &__box {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 6px;
      border: 1px solid #ccc;
      border-radius: 6px;
      padding: 4px 8px;
      min-height: 40px;

      input {
        border: none;
        outline: none;
        flex: 1;
        min-width: 120px;
        font-size: 0.9rem;
        background: transparent;
      }
    }

    &__threshold {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-top: 0.75rem;
      font-size: 0.9rem;

      .threshold-slider {
        flex: 1;
        max-width: 180px;
      }

      .threshold-error {
        color: #a00;
        font-size: 0.8rem;
      }
    }

    &__actions {
      display: flex;
      gap: 0.5rem;
      margin-top: 0.75rem;
    }

    .ingredient-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }

    .ingredient-tag {
      display: flex;
      align-items: center;
      gap: 4px;
      border-radius: 4px;
      padding: 2px 8px;
      font-size: 0.875rem;

      &--mandatory {
        background: #fde8e8;
        color: #a00;
      }

      &--optional {
        background: #e8f4e8;
        color: #2d6a2d;
      }
    }

    .ingredient-tag-type {
      font-size: 0.75rem;
      opacity: 0.7;
    }

    .suggestion {
      align-self: flex-start;
      font-size: 0.8rem;
      color: #555;
    }

    .suggestion-btn {
      background: none;
      border: none;
      padding: 0;
      color: #1a5fb4;
      text-decoration: underline;
      cursor: pointer;
      font-size: inherit;

      &:hover {
        opacity: 0.7;
      }
    }

    .add-new-btn {
      align-self: flex-start;
      font-size: 0.8rem;
      padding: 3px 8px;
      border: 1px dashed #888;
      border-radius: 4px;
      background: none;
      cursor: pointer;
      color: #555;

      &:hover {
        background: #f0f0f0;
      }
    }

    .create-error {
      display: block;
      color: #a00;
      font-size: 0.85rem;
      margin-top: 0.5rem;
    }

    .tag-mode {
      background: none;
      border: 1px solid currentColor;
      border-radius: 3px;
      cursor: pointer;
      font-size: 0.7rem;
      padding: 0 3px;
      line-height: 1.2;
      color: inherit;

      &:hover {
        opacity: 0.7;
      }
    }

    .tag-remove {
      background: none;
      border: none;
      cursor: pointer;
      color: inherit;
      font-size: 1rem;
      padding: 0;
      line-height: 1;

      &:hover {
        opacity: 0.6;
      }
    }

    .search-btn {
      @include button;

      &--dirty {
        outline: 2px solid #2d6a2d;
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .clear-btn {
      font-size: 0.85rem;
      padding: 4px 10px;
      border: 1px solid #ccc;
      border-radius: 4px;
      background: none;
      cursor: pointer;
      color: #666;

      &:hover {
        background: #f5f5f5;
      }
    }

    .cupboard-btn {
      font-size: 0.85rem;
      padding: 4px 10px;
      border: 1px solid #2d6a2d;
      border-radius: 4px;
      background: none;
      cursor: pointer;
      color: #2d6a2d;

      &:hover {
        background: #e8f4e8;
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }
  }
</style>
