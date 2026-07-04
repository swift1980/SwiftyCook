using AutoMapper;
using SwiftCookDb.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Category, Tag, Tool, Ingredient, Unit
        CreateMap<Category, CategoryDto>();
        CreateMap<Tag, TagDto>();
        CreateMap<Tool, ToolDto>();
        CreateMap<Ingredient, IngredientDto>()
            .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src =>
            src.Type != null ? src.Type.Name : null));
        CreateMap<Unit, UnitDto>();

        // Recipe
        CreateMap<Recipe, RecipeReadDto>()
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src =>
                src.RecipeCategories.Select(rc => rc.Category.Name)))
            .ForMember(dest => dest.CategoryIds, opt => opt.MapFrom(src =>
                src.RecipeCategories.Select(rc => rc.CategoryId)))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src =>
                src.RecipeTags.Select(rt => rt.Tag.Name)))
            .ForMember(dest => dest.Tools, opt => opt.MapFrom(src =>
                src.RecipeTools.Select(rt => rt.Tool.Name)))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src =>
                src.RecipeIngredients))
            .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src =>
                src.Instructions.OrderBy(i => i.Position)))
            .ForMember(dest => dest.Nutrition, opt => opt.MapFrom(src => src.Nutrition));

        // RecipeIngredient
        CreateMap<RecipeIngredient, RecipeIngredientReadDto>()
            .ForMember(dest => dest.IngredientId, opt => opt.MapFrom(src => src.IngredientId))
            .ForMember(dest => dest.IngredientName, opt => opt.MapFrom(src => src.Ingredient.Name))
            .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit));

        // RecipeInstruction
        CreateMap<RecipeInstruction, RecipeInstructionReadDto>();

        // Nutrition
        CreateMap<Nutrition, NutritionDto>();

        // ShoppingList
        CreateMap<ShoppingList, ShoppingListDto>()
            .ForMember(dest => dest.IngredientName, opt => opt.MapFrom(src => src.Ingredient.Name))
            .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Unit.Name));

        // Cupboard
        CreateMap<Cupboard, CupboardDto>()
            .ForMember(dest => dest.IngredientName, opt => opt.MapFrom(src => src.Ingredient.Name))
            .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Unit.Name));

        // Recipe Create
        CreateMap<RecipeCreateDto, Recipe>()
            .ForMember(dest => dest.Instructions, opt => opt.Ignore())
            .ForMember(dest => dest.RecipeIngredients, opt => opt.Ignore())
            .ForMember(dest => dest.RecipeCategories, opt => opt.Ignore())
            .ForMember(dest => dest.RecipeTags, opt => opt.Ignore())
            .ForMember(dest => dest.RecipeTools, opt => opt.Ignore());

        CreateMap<RecipeIngredientCreateDto, RecipeIngredient>();

        CreateMap<RecipeInstructionCreateDto, RecipeInstruction>()
            .ForMember(dest => dest.RecipeId, opt => opt.Ignore()); // avoid duplicate tracked entities
    }
}
