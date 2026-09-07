# Implementation Log

Log of changes implemented from `BACKLOG.md`, closing the 5 README feature
gaps identified during the repo review/grilling session. Each ticket was
built on its own local git branch, verified, and merged into `master`.

All commits verified with:
- Backend: `dotnet build` (0 errors) and
  `dotnet test swiftcookapi.tests\swiftcookapi.tests.csproj` (11/11 passing)
- Frontend: `npm run build` (runs `vue-tsc --build` type-check + `vite build`,
  0 errors)

No GitHub Issues/PRs were created — the GitHub MCP tools available in this
environment are read-only, and no `gh` CLI credentials were available.
Branches remain local, ready to push once credentials are configured.

---

## Ticket 1 — Wire recipe name search to backend

**Branch:** `feature/wire-recipe-search` · **Commit:** `b85ff59`
**Closes README feature:** "Searching of Recipes by name"

**Changed files:**
- `swiftcookapi/Controllers/RecipeController.cs` — `Search(q)` now returns
  `RecipeReadDto` (with the same `Include`s as `GetAll`) instead of raw
  `Recipe` entities.
- `swiftcookui/src/composables/useNameSearch.ts` (new) — calls
  `GET /recipe/search?q=`, exposes `results`/`loading`/`error`.
- `swiftcookui/src/stores/recipeStore.ts` — `getFilteredRecipes` replaced
  with `filterByIngredients` (ingredient-tag matching only; name filtering
  is now server-side).
- `swiftcookui/src/views/Recipes.vue` — consumes `nameSearchResults`, falls
  back to the full list (via `filterByIngredients`) when no name query is
  active; now fetches the full recipe list on mount.
- `swiftcookui/src/components/MainContent.vue` — debounces `nameQuery` and
  triggers `useNameSearch().search()`, passes results down to `Recipes.vue`.

**Notable fix (tightly coupled, in scope):** `recipeStore.fetchAllRecipes()`
was never called anywhere in the app, so the browse-all (no query) recipe
list was always empty. Now called from `Recipes.vue`'s `onMounted`.

---

## Ticket 2 — Migrate SPA navigation to vue-router

**Branch:** `feature/router-migration` · **Commit:** `74fe957`
**Infra ticket** (prerequisite for Ticket 3)

**Changed files:**
- `swiftcookui/src/router/index.ts` — added `/recipes/new` (RecipeForm),
  `/shopping-list` (ShoppingList), `/planner` (MealPlanner) routes.
- `swiftcookui/src/App.vue` — removed the local `current` ref; `Sidebar`
  and `MainContent` no longer take/emit `current`.
- `swiftcookui/src/components/Sidebar.vue` — nav buttons replaced with
  `<router-link>` using `active-class="active"` for active-state styling.
- `swiftcookui/src/components/MainContent.vue` — renders the matched route
  via `<router-view v-slot="{ Component }">`; search/filter state
  (`nameQuery`, `ingredientList`, ingredient-search results, etc.) is now
  passed only to the `Recipes` route via a `recipesProps` computed object.

**Deferred:** Cocktails route intentionally left out of this ticket (see
Ticket 3).

---

## Ticket 3 — Add dedicated `/cocktails` route

**Branch:** `feature/cocktails-route` · **Commit:** `069b0a5`
**Closes README feature:** "UI - Different display page for Cocktails
(CategoryId 1)" · **Depends on:** Ticket 2

**Changed files:**
- `swiftcookui/src/router/index.ts` — added `/cocktails` route.
- `swiftcookui/src/components/Sidebar.vue` — added "Cocktails" nav link.
- `swiftcookui/src/components/MainContent.vue` — generalized the
  Recipes-only prop binding into a `routeProps` computed that also covers
  `Cocktails` (passes `nameQuery`/`ingredientList`).

---

## Ticket 4 — Cupboard UI (Pinia store + view)

**Branch:** `feature/cupboard-ui` · **Commit:** `ffed085`
**Closes README feature:** "Cupboard - Track and store ingredients that
user has in stock"

**Note:** Originally planned to mirror `ShoppingList.vue`'s pattern, but
that view turned out to be a static placeholder (hardcoded array, no
store, no API calls) — not a real reference implementation. Built as a
genuinely new, fully working feature instead (confirmed with user
mid-implementation).

**Changed/new files:**
- `swiftcookui/src/interfaces/cupboard.ts` (new) — `CupboardDto` /
  `CupboardCreateDto` matching the backend DTOs.
- `swiftcookui/src/stores/cupboardStore.ts` (new) — `fetchAll` / `addItem`
  / `removeItem` calling the existing `CupboardController` endpoints
  (`GET`/`POST`/`DELETE /cupboard`).
- `swiftcookui/src/views/Cupboard.vue` (new) — ingredient/unit/amount
  picker (reusing `ingredientStore`/`unitStore`, following the
  `RecipeForm.vue` pattern) + list with remove buttons.
- `swiftcookui/src/router/index.ts` — added `/cupboard` route.
- `swiftcookui/src/components/Sidebar.vue` — added "Cupboard" nav link.

---

## Ticket 5 — "Use my cupboard" in Advanced Search

**Branch:** `feature/search-by-cupboard` · **Commit:** `40a1e81`
**Closes README feature:** "Searching of Recipes by ingredients in
cupboard" · **Depends on:** Ticket 4

**Changed files:**
- `swiftcookui/src/components/AdvancedSearch.vue` — added a "Use my
  cupboard" button/action that fetches cupboard contents
  (`cupboardStore`) and prefills them into the existing optional-ingredient
  selection (`mandatory: false` by default), reusing the existing
  `/recipe/search/ingredients` endpoint. Cupboard `Amount` is ignored
  (presence-only matching, not "do I have enough" — explicitly deferred).
  Ingredients not resolvable in `ingredientStore` are skipped so every
  prefilled tag still renders under a type box.

**No backend changes** — frontend-only integration, as agreed.

---

## Merge history

All 5 branches were merged into `master` with `--no-ff` to preserve
per-ticket history:

```
c501732 Merge feature/search-by-cupboard into master
40a1e81 Add 'Use my cupboard' to Advanced Search
e6d4d7f Merge feature/cupboard-ui into master
ffed085 Add Cupboard UI (Pinia store + view)
84e0734 Merge feature/cocktails-route into master
069b0a5 Add dedicated /cocktails route
80fecae Merge feature/router-migration into master
74fe957 Migrate SPA navigation to vue-router
0d542bd Merge feature/wire-recipe-search into master
b85ff59 Wire recipe name search to backend /recipe/search endpoint
```

---

## Explicitly out of scope (deferred, per grilling session decisions)

- Frontend test runner (Vitest) + tests for the new/changed UI code —
  kept as a separate future ticket so these 5 stayed focused.
- Cupboard `Amount`-aware "do I have enough" quantity-sufficiency matching
  for Ticket 5 — presence-only matching implemented instead.
- Fixing `ShoppingList.vue`'s static-stub state — not one of the 5 README
  gaps; flagged as a separate concern discovered during Ticket 4.
- Rotating git-history-committed secrets, CI workflow — carried over from
  the earlier `REVIEW.md` review, still open.
