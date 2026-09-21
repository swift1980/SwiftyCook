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

---

## Backlog v2 (regenerated) — Tickets 1-3

Implemented in a single session (no branch/commit split — see git log for
this session's commits). Verified with:
- Backend: `dotnet build` (0 errors) and
  `dotnet test swiftcookapi.tests\swiftcookapi.tests.csproj` (11/11 passing)
- Frontend: `npm run lint` (0 errors, previously 60) and `npm run build`
  (`vue-tsc --build` + `vite build`, 0 errors)

### Ticket 1 — Wire Shopping List to its existing backend

**Closes:** Shopping List sidebar/route being a hardcoded fake stub.

- `swiftcookui/src/interfaces/shoppingList.ts` (new) — `ShoppingListDto` /
  `ShoppingListCreateDto`.
- `swiftcookui/src/stores/shoppingListStore.ts` (new) — `fetchAll` /
  `addItem` / `removeItem` against `GET`/`POST`/`DELETE /shoppinglist`.
  Deliberately **not** an ingredient-keyed upsert like `cupboardStore` —
  the backend keys shopping list rows by auto-increment `Id`
  (`ShoppingListController.Delete(int id)`), so duplicate ingredient rows
  are allowed by design; every add appends, every remove targets a
  specific row.
- `swiftcookui/src/views/ShoppingList.vue` — replaced the hardcoded
  `['Milk', 'Eggs', ...]` array with a real ingredient/unit/amount picker
  + list + remove buttons, mirroring `Cupboard.vue`.

### Ticket 2 — Remove the Meal Planner stub

**Decision (user-confirmed):** descope rather than build a new backend
domain for it.

- Removed `swiftcookui/src/views/MealPlanner.vue`.
- Removed the `/planner` route from `swiftcookui/src/router/index.ts`.
- Removed the "Meal Planner" nav link from `Sidebar.vue`.

### Ticket 3 — CI workflow

- Added `.github/workflows/ci.yml`: `backend` job (.NET 9.0.x — build +
  test) and `frontend` job (Node 20.x — `npm ci`, `npm run lint`,
  `npm run build`), matching the versions pinned in each service's
  `Dockerfile`. Triggers on `pull_request` (any branch) and `push` to
  `master`. No DB service needed — the backend test suite uses an
  in-memory SQLite context (`RecipeIngredientSearchTestContextFactory.cs`),
  not the real MariaDB.

### Ticket 4 — Frontend test runner + initial store/composable tests

**Closes:** `swiftcookui` having no test runner or unit test coverage for
Pinia stores/composables (`REVIEW.md` issue #6).

- Added `vitest`, `@vue/test-utils`, and `happy-dom` as dev dependencies.
  `@pinia/testing` was evaluated but skipped — it requires Pinia >=4,
  while this repo pins Pinia `^3.0.3`; tests instead call
  `setActivePinia(createPinia())` directly per test.
- Added a `test` script (`vitest run`) to `swiftcookui/package.json`.
- Added a `test` block to `vite.config.ts` (`environment: 'happy-dom'`,
  `globals: true`), switching its `defineConfig` import from `vite` to
  `vitest/config` so Vite's and Vitest's config types merge — required
  for `vue-tsc --build` (used by `npm run build`) to type-check the file
  cleanly.
- Added `src/stores/__tests__/cupboardStore.spec.ts` — `fetchAll` (success
  + error), `addItem` (append + replace-existing-ingredient), `removeItem`
  (success + error, verifying `items` stay unchanged on failure).
- Added `src/composables/__tests__/useNameSearch.spec.ts` and
  `useIngredientSearch.spec.ts` — blank-query no-op guard, success path,
  400-response server-message passthrough, generic-error fallback, and
  `clear`/`loadPage` behavior for the ingredient search. All mock
  `@/services/api` per the ticket's scope.
- 16 tests total, all passing (`npm run test`); `npm run lint` and
  `npm run build` remain 0 errors.
- Wired `npm run test` into `.github/workflows/ci.yml`'s `frontend` job
  (between lint and build) so this coverage runs in CI going forward.

### Pre-existing lint cleanup (surfaced by adding `npm run lint` to CI)

Enabling the existing-but-unused `npm run lint` script for CI surfaced 60
pre-existing errors, all fixed in this session so CI starts green:

- **`@typescript-eslint/no-explicit-any`** (most of the errors): replaced
  `catch (err: any)` with `catch (err: unknown)` across all Pinia stores,
  using a new shared helper `swiftcookui/src/utils/errors.ts`
  (`getErrorMessage(err, fallback)`) instead of unsafe `err.message`
  access. The two ingredient/name-search composables now use
  `axios.isAxiosError(err)` instead of `any` to narrow the 400-response
  case. Untyped `any[]` store state (`categoryStore`, `tagStore`,
  `toolStore`, `unitStore`) was given real DTOs — added
  `interfaces/category.ts` and `interfaces/tag.ts`/`interfaces/tool.ts`
  (didn't exist before) and extended `interfaces/unit.ts` with
  `UnitCreateDto`.
- **`@typescript-eslint/no-unused-vars`**: removed dead imports
  (`cocktailStore.ts`, `recipeStore.ts`), an unused `ref` import
  (`RecipeForm.vue`), and unused `props` locals (`CardGrid.vue`,
  `RecipeCard.vue`, where `defineProps` return value was never read —
  template already consumes props directly). Also fixed
  `tagStore.createTag` / `toolStore.createTool` / `unitStore.createUnit`,
  whose parameters were flagged unused because the POST calls never
  actually sent them as the request body (`api.post('/tag/post')` with no
  payload) — a real bug tightly coupled to the lint fix, now sends the
  typed DTO as the body.
- **`vue/multi-word-component-names`**: added `defineOptions({ name: '...'
  })` with a multi-word name to `Sidebar.vue`, `Cocktails.vue`,
  `Cupboard.vue`, `Recipes.vue` rather than renaming files/routes.
- **`vue/block-lang`**: added `lang="ts"` to `<script setup>` blocks that
  had none (`App.vue`, `Sidebar.vue`, `CardGrid.vue`, `RecipeCard.vue`,
  root-level `PanelExample.vue`, an unused example file). This newly
  exposed real type errors in `CardGrid.vue` under `vue-tsc` (an
  undefined `skeleton` template reference used as a `:key`, and an
  untyped `items` prop) — fixed by keying the skeleton placeholder with a
  static string and typing `items`/`selectedRecipe` as
  `RecipeCardItem[]`/`RecipeCardItem | null` (a type that already existed
  for exactly this shape) instead of `Array`/`any`.
- **`@typescript-eslint/no-empty-object-type`**: `src/shims-vue.d.ts`'s
  generated `DefineComponent<{}, {}, any>` module shim was left as-is with
  a scoped `eslint-disable` comment rather than changed, since altering
  its generic types risked changing type inference for every `.vue` import
  project-wide for no functional benefit.

### Explicitly out of scope (deferred, per this session's decisions)

- Ticket 4 (Vitest + initial store/composable tests) — not implemented
  this session; user chose to limit scope to Tickets 1-3.
- Building a real Meal Planner backend/frontend — descoped instead (see
  Ticket 2 above); left as a future ticket if the feature is wanted.
