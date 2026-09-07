# SwiftCook Backlog

Generated from a grilling session comparing README.md's proposed functionality
against the current state of the repo. Each ticket below is scoped to be an
independent PR; suggested order is noted per ticket.

---

## Ticket 1: Wire recipe name search to the backend endpoint

**Closes README feature:** "Searching of Recipes by name"

**Problem:** `RecipeController.Search(q)` (`swiftcookapi/Controllers/RecipeController.cs`)
already implements name search server-side, but the UI never calls it.
`Recipes.vue` filters an already-loaded, full recipe list client-side via
`getFilteredRecipes(...)`, which won't scale and duplicates the backend logic.

**Scope:**
- Update the recipe name search box (currently wired in `MainContent.vue`) to
  call `GET /recipe/search?q=` instead of filtering client-side.
- Return `RecipeReadDto` (not raw `Recipe` entities) from the endpoint for
  consistency with the rest of the API.
- Remove the now-redundant client-side filtering logic in `Recipes.vue`.
- Keep/verify the existing 400 Bad Request behavior when `q` is missing/blank.

**Order:** No dependencies; can start immediately.

---

## Ticket 2: Migrate SPA navigation to vue-router (infra)

**Problem:** None of the sidebar views (Recipes, Shopping List, Meal Planner,
Cocktails) are backed by real routes. `App.vue` holds a local `current` ref
that `Sidebar.vue` mutates via an `update:current` emit, and `MainContent.vue`
switches views with `v-if="current === ...\"`. This means views can't be
bookmarked/linked, and browser back/forward doesn't work.

**Scope:**
- Add real routes for `/recipes`, `/shopping-list`, `/planner` in
  `swiftcookui/src/router/index.ts` (in addition to the existing `/recipes`
  redirect-from-`/`).
- Replace the `current` ref / `update:current` emit pattern in `App.vue` and
  `Sidebar.vue` with `<router-link>` / `router-view`.
- Update `MainContent.vue` so each view is rendered by the router instead of
  a `v-if` switch.

**Order:** Prerequisite for Ticket 3 (cocktails route). No functional change
to Recipes/Shopping List/Planner beyond routing; keep behavior identical.

---

## Ticket 3: Add a dedicated `/cocktails` route

**Closes README feature:** "UI - Different display page for Cocktails (CategoryId 1)"

**Depends on:** Ticket 2

**Problem:** `Cocktails.vue` exists and calls the cocktail-specific backend
endpoints (`CocktailController.GetAll` / `Search`, both filtered to
`CategoryId == 1`), but it's only reachable via the local view-switch, not a
real route.

**Scope:**
- Add a `/cocktails` route pointing at the existing `Cocktails.vue` view.
- Add a nav link to it from the sidebar (using the router-based nav from
  Ticket 2).

**Order:** After Ticket 2.

---

## Ticket 4: Cupboard UI (Pinia store + view)

**Closes README feature:** "Cupboard - Track and store ingredients that user has in stock"

**Problem:** `CupboardController` (GET/POST/DELETE, `CupboardDto` /
`CupboardCreateDto` with `IngredientId`/`UnitId`/`Amount`) is fully
implemented on the backend, but there is no frontend view, store, or route
for it at all.

**Scope:**
- Add a Pinia store for cupboard state (list/add/remove), mirroring the
  existing ShoppingList store's structure and API-call pattern.
- Add a `Cupboard.vue` view mirroring the existing ShoppingList view's
  ingredient/unit/amount picker + list + remove-button UI.
- Add a `/cupboard` route and sidebar nav link (using the router pattern
  from Ticket 2, or the pre-existing toggle pattern if Ticket 2 hasn't
  landed yet - Ticket 4 has no hard dependency on Ticket 2).

**Order:** No dependency on Tickets 1-3. Prerequisite for Ticket 5.

---

## Ticket 5: "Search by cupboard" in Advanced Search

**Closes README feature:** "Searching of Recipes by ingredients in cupboard"

**Depends on:** Ticket 4

**Problem:** The ingredient-search backend
(`RecipeIngredientSearchService` / `RecipeController.SearchByIngredients`)
only accepts explicit ingredient IDs; nothing connects cupboard contents to
a search. This is a frontend-only integration - no backend changes needed.

**Scope:**
- Add a "Use my cupboard" action/button in `AdvancedSearch.vue`.
- On click, fetch `GET /cupboard` and prefill the ingredient search's
  **optional** ingredient list with the returned ingredient IDs (not
  mandatory - avoids zero-result searches from one missing item). The
  existing threshold slider remains user-adjustable, and users can still
  manually promote items to mandatory or remove them after prefill.
- Cupboard `Amount` is ignored for matching purposes (presence-only, not
  "do I have enough" quantity-sufficiency matching - that's explicitly out
  of scope / deferred as a future enhancement).
- Reuse the existing `/recipe/search/ingredients` endpoint as-is.

**Order:** After Ticket 4.
