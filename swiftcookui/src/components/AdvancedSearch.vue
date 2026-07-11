<template>
  <section class="advanced-search">
    <h3 class="advanced-search__title">Advanced Search</h3>
    <div class="advanced-search__grid">
      <div v-for="box in typeBoxes"
           :key="box.id"
           class="advanced-search__field">
        <label :for="`ingredient-type-${box.id}`">{{ box.name }}</label>

        <div class="advanced-search__box">
          <div v-if="selected[box.id].length" class="ingredient-tags">
            <span v-for="(ing, index) in selected[box.id]"
                  :key="ing"
                  class="ingredient-tag">
              {{ ing }}
              <button type="button"
                      class="tag-remove"
                      @click="removeIngredient(box.id, index)">
                x
              </button>
            </span>
          </div>

          <input :id="`ingredient-type-${box.id}`"
                 v-model="inputs[box.id]"
                 type="text"
                 :list="`ingredient-options-${box.id}`"
                 :placeholder="`Search ${box.name.toLowerCase()}`"
                 @keydown.enter.prevent="addIngredient(box.id)" />
          <datalist :id="`ingredient-options-${box.id}`">
            <option v-for="name in box.options" :key="name" :value="name" />
          </datalist>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
  import { reactive, computed, onMounted, defineProps, defineEmits } from 'vue'
  import { useIngredientStore } from '@/stores/ingredientStore'

  // The three ingredient types to expose as search boxes.
  const TYPE_IDS = [1, 2, 3]

  defineProps({
    modelValue: {
      type: Array as () => string[],
      default: () => []
    }
  })

  const emit = defineEmits(['update:modelValue'])

  const ingredientStore = useIngredientStore()

  // Current input text and selected ingredient names, keyed by type id.
  const inputs = reactive<Record<number, string>>({})
  const selected = reactive<Record<number, string[]>>({})

  TYPE_IDS.forEach((id) => {
    inputs[id] = ''
    selected[id] = []
  })

  // Build a box descriptor per type with its available ingredient names.
  const typeBoxes = computed(() =>
    TYPE_IDS.map((id) => {
      const ingredients = ingredientStore.getIngredientsByTypeId(id)
      return {
        id,
        name: ingredients[0]?.typeName || `Type ${id}`,
        options: ingredients.map((i) => i.name)
      }
    })
  )

  const emitSelection = () => {
    const all = TYPE_IDS.flatMap((id) => selected[id])
    emit('update:modelValue', all)
  }

  const addIngredient = (typeId: number) => {
    const value = inputs[typeId].trim()
    if (!value) return

    // Only accept ingredients that actually belong to this type.
    const match = ingredientStore
      .getIngredientsByTypeId(typeId)
      .find((i) => i.name.toLowerCase() === value.toLowerCase())

    if (match && !selected[typeId].includes(match.name)) {
      selected[typeId].push(match.name)
      emitSelection()
    }
    inputs[typeId] = ''
  }

  const removeIngredient = (typeId: number, index: number) => {
    selected[typeId].splice(index, 1)
    emitSelection()
  }

  onMounted(() => {
    if (!ingredientStore.ingredients.length) {
      ingredientStore.fetchAll()
    }
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

    .ingredient-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }

    .ingredient-tag {
      display: flex;
      align-items: center;
      gap: 4px;
      background: #e8f4e8;
      color: #2d6a2d;
      border-radius: 4px;
      padding: 2px 8px;
      font-size: 0.875rem;
    }

    .tag-remove {
      background: none;
      border: none;
      cursor: pointer;
      color: #2d6a2d;
      font-size: 1rem;
      padding: 0;
      line-height: 1;

      &:hover {
        color: #a00;
      }
    }
  }
</style>
