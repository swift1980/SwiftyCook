<template>
  <div class="planner">
    <h2>Meal Planner</h2>

    <div class="nav">
      <button type="button" @click="store.previousWeek()">← Previous</button>
      <button type="button" @click="store.thisWeek()">This week</button>
      <button type="button" @click="store.nextWeek()">Next →</button>
      <span class="range">{{ store.weekDays[0] }} – {{ store.weekDays[6] }}</span>
    </div>

    <div v-if="narrow" class="day-tabs">
      <button
        v-for="(d, i) in store.weekDays"
        :key="d"
        type="button"
        :class="{ active: i === dayIndex }"
        @click="dayIndex = i"
      >{{ dayLabel(d) }}</button>
    </div>

    <div v-if="store.error" class="error">{{ store.error }}</div>

    <div class="grid" :style="{ gridTemplateColumns: `6rem repeat(${visibleDays.length}, minmax(0, 1fr))` }">
      <div class="corner"></div>
      <div v-for="d in visibleDays" :key="d" class="day-head" :class="{ today: d === today }">{{ dayLabel(d) }}</div>

      <template v-for="meal in MEAL_TYPES" :key="meal">
        <div class="meal-head">{{ meal }}</div>
        <div v-for="d in visibleDays" :key="d + meal" class="cell" :data-cell="`${d}|${meal}`">
          <div v-for="e in store.entriesFor(d, meal)" :key="e.id" class="entry">
            <span class="entry-name">{{ e.recipeName }}</span>
            <input
              class="entry-servings"
              type="number"
              min="1"
              max="1000"
              :value="e.servings"
              :aria-label="`Servings for ${e.recipeName}`"
              @change="onServings(e, $event)"
            />
            <button type="button" class="remove" :aria-label="`Remove ${e.recipeName}`" @click="store.removeEntry(e.id)">✕</button>
          </div>
          <button type="button" class="add" :aria-label="`Add to ${meal} on ${d}`" @click="openDialog(d, meal)">+</button>
        </div>
      </template>
    </div>

    <MealPlanAddDialog
      v-if="dialog"
      :date="dialog.date"
      :meal-type="dialog.mealType"
      @close="dialog = null"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import MealPlanAddDialog from '@/components/MealPlanAddDialog.vue'
import { useMealPlanStore } from '@/stores/mealPlanStore'
import { MEAL_TYPES, type MealPlanEntryDto, type MealType } from '@/interfaces/mealPlan'
import { parseIsoDate, todayIso } from '@/utils/dates'

const store = useMealPlanStore()
const today = todayIso()

const dialog = ref<{ date: string; mealType: MealType } | null>(null)

// Narrow screens show a single day at a time.
const narrow = ref(false)
const dayIndex = ref(0)
let query: MediaQueryList | null = null
const onQueryChange = (e: MediaQueryListEvent) => { narrow.value = e.matches }

const visibleDays = computed(() => (narrow.value ? [store.weekDays[dayIndex.value]] : store.weekDays))

function dayLabel(iso: string) {
  return parseIsoDate(iso).toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short' })
}

function openDialog(date: string, mealType: MealType) {
  dialog.value = { date, mealType }
}

async function onServings(entry: MealPlanEntryDto, event: Event) {
  const input = event.target as HTMLInputElement
  const value = Math.floor(Number(input.value))
  if (!Number.isFinite(value) || value < 1 || value > 1000) {
    input.value = String(entry.servings)
    return
  }
  try {
    await store.updateServings(entry, value)
  } catch {
    input.value = String(entry.servings)
  }
}

onMounted(() => {
  if (typeof window.matchMedia === 'function') {
    query = window.matchMedia('(max-width: 700px)')
    narrow.value = query.matches
    query.addEventListener('change', onQueryChange)
  }
  store.fetchWeek()
})

onBeforeUnmount(() => query?.removeEventListener('change', onQueryChange))
</script>

<style scoped>
.planner { padding: 1rem; }
.nav { display: flex; gap: 0.5rem; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; }
.nav button, .day-tabs button { padding: 0.4rem 0.8rem; border: 1px solid #ccc; border-radius: 0.5rem; background: #fff; cursor: pointer; }
.range { color: #666; }
.day-tabs { display: flex; gap: 0.25rem; margin-bottom: 0.75rem; overflow-x: auto; }
.day-tabs button.active { background: #374151; color: #fff; }
.grid { display: grid; gap: 0.25rem; }
.day-head, .meal-head { font-weight: 600; padding: 0.25rem; }
.day-head.today { background: #fef3c7; }
.cell { border: 1px solid #eee; border-radius: 0.5rem; padding: 0.25rem; min-height: 3.5rem; display: flex; flex-direction: column; gap: 0.25rem; }
.entry { display: flex; align-items: center; gap: 0.25rem; background: #f3f4f6; border-radius: 0.4rem; padding: 0.2rem 0.3rem; font-size: 0.85rem; }
.entry-name { flex: 1; min-width: 0; overflow-wrap: anywhere; }
.entry-servings { width: 3rem; }
.remove { background: none; border: none; cursor: pointer; color: #a00; }
.add { align-self: flex-start; background: none; border: 1px dashed #bbb; border-radius: 0.4rem; cursor: pointer; padding: 0 0.5rem; }
.error { color: #a00; margin-bottom: 0.5rem; }
</style>
