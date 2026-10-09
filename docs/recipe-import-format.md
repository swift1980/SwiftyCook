# Recipe import format (v1)

Recipes are imported from a JSON file holding **one recipe object** or an
**array of recipe objects**. The machine-readable schema is
[`recipe-import.schema.json`](recipe-import.schema.json); a complete example
is [`recipe-import.example.json`](recipe-import.example.json).

Names, not ids, are used everywhere. They are resolved against the database
at import time (Ticket 16): case-insensitive exact match first, then near
matches, which the user confirms in the dry-run preview.

## Fields

| Field | Type | Required | Notes |
|---|---|---|---|
| `name` | string, 1-50 | yes | A name that already exists produces a warning; the importer never overwrites. |
| `description` | string, max 5000 | no | |
| `image` | string, URL, max 255 | no | Stored as-is. Never downloaded. |
| `prepTime` | integer >= 0 | no | Minutes. |
| `cookTime` | integer >= 0 | no | Minutes. |
| `yield` | integer >= 1 | no | Number of servings. |
| `categories` | string[] (1-50 each) | no | Unknown names are created. |
| `tags` | string[] (1-50 each) | no | Unknown names are created. |
| `tools` | string[] (1-50 each) | no | Unknown names are created. |
| `ingredients` | object[] | yes, at least 1 | See below. |
| `instructions` | string[] (1-5000 each) | yes, at least 1 | Order in the array is the step order. |
| `source` | object | no | `url` (max 500), `title` (max 255), `author` (max 255). |

### Ingredient lines

| Field | Type | Notes |
|---|---|---|
| `name` | string, 1-50, required | Unknown names need a user decision in the preview (map to an existing ingredient or create one). |
| `amount` | number >= 0, or omitted | Omit for "to taste" style items. Decimals, not fractions (`0.5`, not `1/2`). |
| `unit` | string, max 50 | Name or abbreviation, e.g. `g`, `tbsp`, `fl oz`. Omit for counted items ("2 onions"). Unknown units block that line until mapped; units are never created. |
| `note` | string, max 255 | Free text such as `finely chopped`. Not used for searching. |

Unit guidance (UK/metric):

- Use `oz` only for weight and `fl oz` for fluid ounces.
- Spoons and cups are metric: tsp 5 ml, tbsp 15 ml, cup 250 ml.
- Prefer `g`, `kg`, `ml` and `l` when the source gives them.

## Limits

| Limit | Value |
|---|---|
| File size | 1 MB |
| Recipes per file | 50 |
| Ingredients per recipe | 100 |
| Instructions per recipe | 100 |
| Categories / tags / tools per recipe | 20 each |
| Unknown properties | rejected |

The upload is validated against these limits and the schema before any name
resolution. A recipe that fails validation is reported and skipped; other
recipes in the file are unaffected.

## Prompt template

Paste the following into an LLM, followed by the recipe text or page content.

```text
Convert the recipe below into JSON that follows this format exactly.
Output only the JSON, with no commentary or code fences.

- Root: one recipe object.
- Fields: name, description, image, prepTime, cookTime (minutes), yield
  (integer servings), categories, tags, tools (arrays of short names),
  ingredients, instructions (array of strings, one per step, no numbering),
  source { url, title, author }.
- Each ingredient: { "name", "amount", "unit", "note" }.
  - name: the plain ingredient ("onion", not "finely chopped onion").
  - amount: a decimal number (0.5, not 1/2). Omit it for "to taste".
  - unit: g, kg, ml, l, tsp, tbsp, cup, oz (weight), fl oz (fluid), pint, lb.
    Omit for counted items. Convert other units where practical.
  - note: preparation or qualifier ("finely chopped", "to taste"). Omit if none.
- Use UK/metric: tsp = 5 ml, tbsp = 15 ml, cup = 250 ml.
- Omit any field you cannot determine; never invent values.
- Maximum 100 ingredients and 100 steps.

Recipe:
<paste here>
```
