SwiftCook (T2) Repository Review
Generated: 2026-09-07

================================================================
SUMMARY OF FUNCTIONALITY
================================================================

SwiftCook is a recipe/cocktail management app:

- swiftcookapi   - ASP.NET Core Web API (EF Core + Pomelo/MariaDB) exposing
                   CRUD controllers for Recipes, Ingredients, Categories,
                   Tags, Tools, Units, Cupboard (pantry inventory), Shopping
                   List, and a dedicated ingredient-based recipe search
                   (find recipes by mandatory/optional ingredients with a
                   match threshold, paginated).
- swiftcookdb    - EF Core models + init.sql schema + seed SQL data (units,
                   ingredients, cocktails, tools, etc.).
- swiftcookimporter - console tool that imports cocktails.csv into the DB.
- swiftcookui    - Vue 3 + TypeScript + Pinia + Tailwind SPA (Recipes,
                   Cocktails, Meal Planner, Shopping List views), calling
                   the API via axios (/api proxied through nginx in prod,
                   Vite dev proxy locally).
- docker-compose.yml wires UI + API + MariaDB together, auto-seeding the
  DB on first run.

The ingredient-search feature is the most mature part of the codebase - it
has a real service layer, DTOs, an outcome/result pattern, and a solid
xUnit test suite covering dedup, thresholds, ranking, and pagination edge
cases.

================================================================
FIXES APPLIED (2026-09-07)
================================================================

1. Secrets removed from docker-compose.yml - now sourced from .env via
   ${DB_ROOT_PASSWORD}/${DB_USER}/${DB_PASSWORD}/${DB_NAME}; added
   .env.example with safe placeholders. NOTE: the real password that was
   committed to git history must still be rotated.

   Also discovered and fixed a related bug: appsettings.json contained a
   plaintext DB password in a MALFORMED connection string (missing the
   "Password=" key entirely, so it would have failed to authenticate),
   and the DB_HOST/DB_USER/DB_PASSWORD/DB_NAME env vars set in
   docker-compose.yml for the API container were never read by the app.
   Fixed by having Program.cs build the connection string from those env
   vars when present (docker), falling back to
   ConnectionStrings:SwiftCookDatabase in appsettings.json (local/non-
   docker dev, now a non-secret localhost placeholder).

3. Added existence checks (404) to every PUT endpoint before
   Entry(x).State = Modified, across Category, Tag, Tool, Unit,
   IngredientType, Ingredient, Recipe, and Cocktail controllers.

4. RecipeController.Search / CocktailController.Search now return
   400 Bad Request when the 'q' query parameter is missing/blank.

Verified via `dotnet build` (0 errors) and `dotnet test`
(11/11 passing).

Not yet done (larger follow-up work, deferred):
   - #5 CI workflow (GitHub Actions).
   - #6 Frontend test runner (Vitest) + initial store tests.

2. Added Create DTOs (CategoryCreateDto, TagCreateDto, ToolCreateDto,
   IngredientTypeCreateDto, UnitCreateDto, IngredientCreateDto,
   CupboardCreateDto, ShoppingListCreateDto, reusing RecipeCreateDto for
   Recipe/Cocktail) so POST/PUT actions on Category, Tag, Tool,
   IngredientType, Unit, Ingredient, Cupboard, ShoppingList, Recipe, and
   Cocktail controllers no longer bind raw EF entities from the request
   body - clients can no longer set Id/FK/navigation properties directly
   (mass-assignment/overposting). AutoMapper CreateMap entries added in
   MappingProfile.cs (nav properties explicitly ignored on the DTO ->
   entity maps). PUT actions now fetch the tracked entity first and map
   the DTO onto it (also folds in the #3 404 checks, replacing the
   earlier AnyAsync-based check + Entry(x).State = Modified pattern).
   CocktailController.Create was also switched from raw Recipe binding to
   RecipeCreateDto (mirroring RecipeController), and now always tags new
   cocktails with CategoryId 1 so they still appear in the cocktails
   list.

   Re-verified via `dotnet build` (0 errors) and `dotnet test`
   (11/11 passing) after this change.

7. CORS allowed origins are now config-driven via Cors:AllowedOrigins in
   appsettings.json (defaults to http://localhost:5173 for local dev)
   instead of a hardcoded value in Program.cs. docker-compose.yml sets
   Cors__AllowedOrigins__0 for the API container, defaulting to
   http://localhost:8080 (the UI's compose port) and overridable via the
   new API_CORS_ORIGIN variable in .env.

   Verified via `dotnet build` (0 errors), `dotnet test` (11/11 passing),
   and a manual smoke test: started the API locally and confirmed via
   curl OPTIONS preflight requests that the configured origin
   (http://localhost:5173) receives Access-Control-Allow-Origin while an
   arbitrary origin (http://evil.example.com) does not.

================================================================
ISSUES / BUGS FOUND
================================================================

1. Hardcoded secrets committed to git
   docker-compose.yml (tracked, 1 commit in) contains plaintext MariaDB
   root/user passwords (C@k31S4713, GL@D0s). These should move to a .env
   file (already gitignored) referenced via ${VAR} substitution, and the
   committed values should be rotated since they're now in history.

2. Mass-assignment / overposting risk
   Nearly every POST/PUT action (Category, Tag, Ingredient, Cupboard,
   ShoppingList controllers, and Recipe.Update) binds the raw EF entity
   directly from the request body instead of a Create/Update DTO. Clients
   can set any field (including PKs/FKs) directly, and
   Entry(x).State = Modified blindly overwrites all columns even if
   omitted from the payload (they'll be reset to default/null).
   RecipeController.CreateRecipe is the one exception, using a proper
   DTO/mapping approach - the rest should follow that pattern.

3. No input validation on PUT endpoints
   Updates don't check the entity exists first; a mismatched/nonexistent
   id throws a DbUpdateConcurrencyException (unhandled) rather than
   returning a clean 404.

4. RecipeController.Search
   Takes string q with no [Required]/null-check - an omitted query
   string silently returns Contains(null) behavior rather than a clear
   400.

5. No automated CI
   .github/ only contains Copilot instructions/prompts, no GitHub Actions
   workflow to build/test on push or PR, so regressions (backend or
   frontend) aren't automatically caught.

6. No frontend tests
   package.json has no test runner (no Vitest/Jest) or test script;
   Pinia stores, views, and API integration are entirely unverified.

7. CORS policy is hardcoded to http://localhost:5173 in Program.cs - fine
   for local dev (nginx same-origin proxy handles prod), but brittle if
   the API is ever called directly from another origin/environment;
   should be config-driven.

================================================================
AREAS NOT ASSESSED
================================================================

- Runtime/functional behavior - reviewed code statically only; did not
  build, run, or hit the API/UI (no dotnet build / npm run dev /
  docker compose up executed), so cannot confirm the app actually runs
  end-to-end or that the DB schema matches the EF models exactly.
- .env file contents - present but gitignored; not inspected (may
  contain live secrets).
- swiftcookui/dist, bin, obj, node_modules - build artifacts, skipped.
- Full frontend component logic (RecipeForm.vue, MealPlanner.vue, etc.) -
  only listed, not read in depth; UI-specific bugs may exist there.
- swiftcookimporter internals and cocktails.csv data quality - not
  reviewed in detail.
- .agents/skills and skills-lock.json - tooling config, not part of app
  functionality, not assessed.
- Git history beyond the most recent commits - only spot-checked
  docker-compose.yml history.
