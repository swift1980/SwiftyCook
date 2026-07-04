using AutoMapper;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;
using System.Globalization;
using System.Text;

namespace SwiftCook.Importer
{
    public class RecipeImporterService
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;
        private readonly StringBuilder _logBuilder = new();

        public RecipeImporterService(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task ImportFromCsvAsync(string csvPath)
        {
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"File not found: {csvPath}");
                return;
            }

            var rows = ParseCsv(csvPath);
            Console.WriteLine($"Found {rows.Count} recipes to import.\n");
            _logBuilder.AppendLine($"SwiftCook Import Log - {DateTime.Now}");
            _logBuilder.AppendLine(new string('=', 50));

            foreach (var row in rows)
            {
                try
                {
                    var dto = await BuildRecipeCreateDtoAsync(row);
                    // Map to entity
                    var recipe = _mapper.Map<Recipe>(dto);
                    recipe.NameNormalized = recipe.Name.ToLowerInvariant();
                    recipe.DescriptionNormalized = recipe.Description?.ToLowerInvariant();

                    _context.Recipes.Add(recipe);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Imported recipe: {recipe.Name}");
                    _logBuilder.AppendLine($"Imported recipe: {recipe.Name}");

                    // Ingredients
                    if (dto.Ingredients?.Any() == true)
                    {
                        var recipeIngredients = dto.Ingredients.Select((riDto, index) =>
                        {
                            var ri = _mapper.Map<RecipeIngredient>(riDto);
                            ri.RecipeId = recipe.Id;
                            ri.Position = riDto.Position ?? (index + 1);
                            return ri;
                        }).ToList();

                        _context.RecipeIngredients.AddRange(recipeIngredients);
                    }

                    // Instructions
                    if (dto.Instructions?.Any() == true)
                    {
                        var instructions = dto.Instructions.Select((stepDto, index) =>
                        {
                            var step = _mapper.Map<RecipeInstruction>(stepDto);
                            step.RecipeId = recipe.Id;
                            step.Position = stepDto.Position > 0 ? stepDto.Position : (index + 1);
                            return step;
                        }).ToList();

                        _context.RecipeInstructions.AddRange(instructions);
                    }

                    // Categories
                    if (dto.CategoryIds?.Any() == true)
                    {
                        var recipeCategories = dto.CategoryIds.Select(catId => new RecipeCategory
                        {
                            RecipeId = recipe.Id,
                            CategoryId = catId
                        });
                        _context.RecipeCategories.AddRange(recipeCategories);
                    }

                    // Tags
                    if (dto.TagIds?.Any() == true)
                    {
                        var recipeTags = dto.TagIds.Select(tagId => new RecipeTag
                        {
                            RecipeId = recipe.Id,
                            TagId = tagId
                        });
                        _context.RecipeTags.AddRange(recipeTags);
                    }

                    // Tools
                    if (dto.ToolIds?.Any() == true)
                    {
                        var recipeTools = dto.ToolIds.Select(toolId => new RecipeTool
                        {
                            RecipeId = recipe.Id,
                            ToolId = toolId
                        });
                        _context.RecipeTools.AddRange(recipeTools);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to import {row.Name}: {ex.Message}");
                    _logBuilder.AppendLine($"Failed to import {row.Name}: {ex.Message}");
                }
            }

            Console.WriteLine("\nImport complete!");
            await WriteLogAsync();
        }

        // --- Parse CSV rows ---
        private List<RawRecipeCsv> ParseCsv(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<RawRecipeCsv>().ToList();
        }

        // --- Convert CSV row -> DTO ---
        private async Task<RecipeCreateDto> BuildRecipeCreateDtoAsync(RawRecipeCsv row)
        {
            var dto = new RecipeCreateDto
            {
                Name = row.Name,
                Description = row.Description,
                PrepTime = row.PrepTime,
                CookTime = row.CookTime,
                Yield = row.Yield,
                Ingredients = await ParseIngredientsAsync(row.Ingredients),
                Instructions = ParseInstructions(row.Instructions),
                CategoryIds = await GetCategoryIdsAsync(row.Categories),
                TagIds = await GetTagIdsAsync(row.Tags),
                ToolIds = await GetToolIdsAsync(row.Tools)
            };

            return dto;
        }

        // --- Ingredient helpers ---
        private async Task<List<RecipeIngredientCreateDto>> ParseIngredientsAsync(string raw)
        {
            var list = new List<RecipeIngredientCreateDto>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            int pos = 1;

            foreach (var p in parts)
            {
                var segs = p.Split('|', StringSplitOptions.TrimEntries);
                if (segs.Length < 3) continue;

                string ingredientName = segs[0];
                float? amount = float.TryParse(segs[1], out var amt) ? amt : null;
                string unitName = segs[2];

                // Ensure ingredient exists
                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Name.ToLower().Equals(ingredientName.ToLower()));
                if (ingredient == null)
                {
                    ingredient = new Ingredient { Name = ingredientName };
                    _context.Ingredients.Add(ingredient);
                    await _context.SaveChangesAsync();
                    _logBuilder.AppendLine($"Created ingredient '{ingredientName}'");
                }

                // Ensure unit exists
                var unit = await _context.Units
                    .FirstOrDefaultAsync(u => u.Name.ToLower() == unitName.ToLower());

                if (unit == null)
                {
                    _logBuilder.AppendLine($"Unit '{unitName}' not found, skipping ingredient '{ingredientName}'");
                    continue;
                }

                list.Add(new RecipeIngredientCreateDto
                {
                    IngredientId = ingredient.Id,
                    Amount = amount,
                    UnitId = unit.Id,
                    Position = pos++
                });
            }

            return list;
        }

        // --- Instruction helpers ---
        private List<RecipeInstructionCreateDto> ParseInstructions(string raw)
        {
            var list = new List<RecipeInstructionCreateDto>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            var steps = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < steps.Length; i++)
            {
                list.Add(new RecipeInstructionCreateDto
                {
                    Step = steps[i].Trim(),
                    Position = i + 1
                });
            }

            return list;
        }

        // --- Category helpers ---
        private async Task<List<int>> GetCategoryIdsAsync(string? raw)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(raw)) return ids;

            var names = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var name in names)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

                if (category != null)
                    ids.Add(category.Id);
                else
                    Console.WriteLine($"Category '{name}' not found, skipping.");
            }

            return ids;
        }

        // --- Tag helpers ---
        private async Task<List<int>> GetTagIdsAsync(string? raw)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(raw)) return ids;

            var names = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var name in names)
            {
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

                if (tag == null)
                {
                    tag = new Tag { Name = name };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }

                ids.Add(tag.Id);
            }

            return ids;
        }

        // --- Tag helpers ---
        private async Task<List<int>> GetToolIdsAsync(string? raw)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(raw)) return ids;

            var names = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var name in names)
            {
                var tool = await _context.Tools
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

                if (tool == null)
                {
                    tool = new Tool { Name = name };
                    _context.Tools.Add(tool);
                    await _context.SaveChangesAsync();
                }

                ids.Add(tool.Id);
            }

            return ids;
        }

        // --- Write log file ---
        private async Task WriteLogAsync()
        {
            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "import_log.txt");
            await File.WriteAllTextAsync(logPath, _logBuilder.ToString());
        }
    }


    // --- Model representing raw CSV row ---
    public class RawRecipeCsv
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PrepTime { get; set; }
        public int CookTime { get; set; }
        public int Yield { get; set; }
        public string Ingredients { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string? Categories { get; set; }
        public string? Tags { get; set; }
        public string? Tools { get; set; }
    }
}
