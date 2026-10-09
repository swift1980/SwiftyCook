<template>
  <div>
    <CardGrid v-if="displayItems.length || loading"
              :items="displayItems"
              :loading="loading" />

    <div v-else-if="emptyState" class="empty-state">{{ emptyState }}</div>
    <div v-else-if="searchError" class="search-error">
      {{ searchError }}
      <button class="retry-btn" @click="emit('retry')">Retry</button>
    </div>
    <div v-else-if="storeError" class="search-error">{{ storeError }}</div>
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted } from 'vue'
  import { useCocktailStore } from '@/stores/cocktailStore'
  import { storeToRefs } from 'pinia'
  import CardGrid from '@/components/shared/CardGrid.vue'
  import type { PagedResultDto, RecipeSearchResultDto, RecipeCardItem } from '@/interfaces/ingredientSearch'

  defineOptions({ name: 'CocktailsView' })

  const cocktailStore = useCocktailStore();
  const { loading: storeLoading, error: storeError } = storeToRefs(cocktailStore);

  const props = defineProps({
    nameQuery: { type: String, default: '' },
    searchResults: { type: Object as () => PagedResultDto<RecipeSearchResultDto> | null, default: null },
    searchError: { type: String as () => string | null, default: null },
    searchLoading: { type: Boolean, default: false },
  })

  const emit = defineEmits<{ (e: 'retry'): void }>()

  onMounted(() => {
    cocktailStore.fetchCocktailAll();
  });

  const loading = computed(() => {
    if (props.searchResults !== null) return props.searchLoading
    return storeLoading.value
  })

  /** Normalise RecipeSearchResultDto → RecipeCardItem for CardGrid (Ticket 5, mirrors Recipes.vue). */
  function normalise(dto: RecipeSearchResultDto): RecipeCardItem {
    return {
      id: dto.recipeId,
      name: dto.name,
      image: dto.image,
      optionalMatchCount: dto.optionalMatchCount,
      optionalTotal: dto.optionalTotal,
      matchedOptionalIds: dto.matchedOptionalIds,
      missingCount: dto.missingIngredientCount,
    }
  }

  const displayItems = computed((): RecipeCardItem[] => {
    if (props.searchResults !== null) {
      const normalised = props.searchResults.items.map(normalise)
      if (!props.nameQuery) return normalised
      const q = props.nameQuery.toLowerCase()
      return normalised.filter((c) => c.name.toLowerCase().includes(q))
    }

    // Browse-all/name-filter path — unchanged client-side name filter.
    return cocktailStore.getFilteredCocktails(props.nameQuery).map((c) => ({ id: c.id, name: c.name, image: c.image }))
  })

  const emptyState = computed((): string | null => {
    if (loading.value || props.searchError || storeError.value) return null

    if (props.searchResults !== null) {
      if (props.searchResults.totalCount === 0) {
        return 'No cocktails matched these ingredients — try broadening your search.'
      }
      if (displayItems.value.length === 0 && props.nameQuery) {
        return `No results matching "${props.nameQuery}" in these ingredients — try clearing the name filter.`
      }
    } else if (props.nameQuery && displayItems.value.length === 0) {
      return `No cocktails matched "${props.nameQuery}".`
    }
    return null
  })
</script>
