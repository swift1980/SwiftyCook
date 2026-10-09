<template>
  <section class="cooklog">
    <h3>Cook log</h3>
    <p v-if="lastMade" class="last-made">{{ lastMade }}</p>
    <p v-else-if="!store.loading" class="last-made">Not made yet</p>
    <div v-if="store.error" class="error" role="alert">{{ store.error }}</div>

    <form class="form" @submit.prevent="submit">
      <label>Date <input v-model="cookedOn" type="date" :max="today" required /></label>
      <label>Servings <input v-model.number="servings" type="number" min="1" max="1000" required /></label>
      <label class="notes">Notes <input v-model="notes" type="text" maxlength="500" /></label>
      <button type="submit" :disabled="!valid">{{ editing ? 'Save' : 'Log as made' }}</button>
      <button v-if="editing" type="button" @click="resetForm">Cancel</button>
    </form>

    <ul v-if="store.logs.length" class="history">
      <li v-for="l in store.logs" :key="l.id">
        <span>{{ l.cookedOn }} · {{ l.servings }} serving{{ l.servings === 1 ? '' : 's' }}<template v-if="l.notes"> · {{ l.notes }}</template></span>
        <button type="button" @click="startEdit(l)">Edit</button>
        <template v-if="confirmId === l.id">
          <button type="button" class="danger" @click="doDelete(l)">Confirm delete</button>
          <button type="button" @click="confirmId = null">Cancel</button>
        </template>
        <button v-else type="button" @click="confirmId = l.id">Delete</button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useCookLogStore } from '@/stores/cookLogStore'
import type { CookLogDto } from '@/interfaces/cookLog'
import { todayIso } from '@/utils/dates'
import { lastMadeText } from '@/utils/cookLog'

const props = defineProps<{ recipeId: number; defaultServings: number }>()

const store = useCookLogStore()
const today = todayIso()
const defaultServings = () => Math.max(props.defaultServings || 1, 1)

const cookedOn = ref(today)
const servings = ref(defaultServings())
const notes = ref('')
const editing = ref<CookLogDto | null>(null)
const confirmId = ref<number | null>(null)

const lastMade = computed(() => lastMadeText(store.logs.map(l => l.cookedOn)))
const valid = computed(() =>
  !!cookedOn.value && cookedOn.value <= today && Number.isInteger(servings.value) && servings.value >= 1 && servings.value <= 1000)

function resetForm() {
  editing.value = null
  cookedOn.value = today
  servings.value = defaultServings()
  notes.value = ''
}

function startEdit(l: CookLogDto) {
  editing.value = l
  cookedOn.value = l.cookedOn
  servings.value = l.servings
  notes.value = l.notes ?? ''
}

async function submit() {
  if (!valid.value) return
  const dto = { cookedOn: cookedOn.value, servings: servings.value, notes: notes.value.trim() || null }
  try {
    if (editing.value) await store.update(editing.value, dto)
    else await store.log({ recipeId: props.recipeId, ...dto })
    resetForm()
  } catch {
    // The store exposes the error message.
  }
}

async function doDelete(l: CookLogDto) {
  confirmId.value = null
  try {
    await store.remove(l)
    if (editing.value?.id === l.id) resetForm()
  } catch {
    // The store exposes the error message.
  }
}

onMounted(() => store.fetchForRecipe(props.recipeId))
watch(() => props.recipeId, id => { resetForm(); store.fetchForRecipe(id) })
</script>

<style scoped>
.cooklog { margin-top: 1rem; }
.last-made { color: #374151; }
.form { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: end; margin: 0.5rem 0; }
.form label { display: flex; flex-direction: column; font-size: 0.85rem; }
.form .notes { flex: 1; min-width: 8rem; }
.form input[type=number] { width: 4.5rem; }
.history { list-style: none; padding: 0; }
.history li { display: flex; gap: 0.5rem; align-items: center; padding: 0.2rem 0; }
.history li span { flex: 1; overflow-wrap: anywhere; }
.danger { color: #a00; }
.error { color: #a00; }
</style>
