<template>
  <section class="advanced-search">
    <h3 class="advanced-search__title">Advanced Search</h3>

    <div class="advanced-search__grid">
      <div v-for="box in typeBoxes" :key="box.typeId" class="advanced-search__field">
        <label :for="`ingredient-type-${box.typeId}`">{{ box.typeName }}</label>

        <div class="advanced-search__box">
          <div v-if="selectedByType[box.typeId]?.length" class="ingredient-tags">
            <span v-for="item in selectedByType[box.typeId]"
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
              <button type="button" class="tag-remove" @click="removeIngredient(item.id)">x</button>
            </span>
          </div>

          <input :id="`ingredient-type-${box.typeId}`"
                 v-model="inputs[box.typeId]"
                 type="text"
                 :list="`ingredient-options-${box.typeId}`"
                 :placeholder="`Search ${box.typeName.toLowerCase()}`"
                 @keydown.enter.prevent="addIngredient(box.typeId)" />
          <datalist :id="`ingredient-options-${box.typeId}`">
            <option v-for="name in box.options" :key="name" :value="name" />
          </datalist>
        </div>
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
  import { useCupboardStore } from '@/stores/cupboardStore'
  import type { IngredientSearchParams } from '@/interfaces/ingredientSearch'

  const emit = defineEmits<{
    (e: 'search', params: IngredientSearchParams): void
    (e: 'clear'): void
  }>()

  const ingredientStore = useIngredientStore()
  const cupboardStore = useCupboardStore()

  interface SelectedIngredient {
    id: number
    name: string
    mandatory: boolean
  }

  // Keyed by typeId: current text input value
  const inputs = reactive<Record<number, string>>({})

  // All selected ingredients flat, keyed by ingredient id for O(1) lookup
  const selectedMap = reactive<Record<number, SelectedIngredient>>({})

  const threshold = ref(0)
  const isDirty = ref(false)
  const lastSubmittedKey = ref('')

  const typeBoxes = computed(() =>
    ingredientStore.getIngredientsGroupedByTypeWithNames.map((group) => ({
      typeId: group.type.id,
      typeName: group.type.name,
      options: group.ingredients.map((i) => i.name),
    }))
  )

  // Initialise inputs when typeBoxes resolves
  watch(typeBoxes, (boxes) => {
    boxes.forEach((b) => {
      if (!(b.typeId in inputs)) inputs[b.typeId] = ''
    })
  }, { immediate: true })

  const selectedByType = computed(() => {
    const map: Record<number, SelectedIngredient[]> = {}
    for (const item of Object.values(selectedMap)) {
      const typeId = ingredientStore.ingredients.find((i) => i.id === item.id)?.typeId
      if (typeId == null) continue
      if (!map[typeId]) map[typeId] = []
      map[typeId].push(item)
    }
    return map
  })

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

  function addIngredient(typeId: number) {
    const value = inputs[typeId]?.trim()
    if (!value) return
    const match = ingredientStore
      .getIngredientsByTypeId(typeId)
      .find((i) => i.name.toLowerCase() === value.toLowerCase())
    if (match && !(match.id in selectedMap)) {
      selectedMap[match.id] = { id: match.id, name: match.name, mandatory: false }
    }
    inputs[typeId] = ''
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
      // Only add ingredients we can resolve a type for, so they render in a type box
      const known = ingredientStore.ingredients.some((i) => i.id === item.ingredientId)
      if (!known) continue
      selectedMap[item.ingredientId] = { id: item.ingredientId, name: item.ingredientName, mandatory: false }
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
