namespace recipe_catalog_api.Models;

public class Recipe
{
    public int Id { get; set; }

    public string Name { get; set; }

    public List<string> CourseTypes { get; set; }

    public List<string> CuisineTypes { get; set; }

    public List<string> Tags { set; get; }

    public int ServingAmount { get; set; }

    public string ServingName { get; set; }

    public string? Source { get; set; }

    public DateTime? Uploaded { get; set; }

    public int? ParentId { get; set; }

    public List<Recipe> SubRecipes { get; set; }

    public Recipe(int Id, string Name, int ServingAmount, string ServingName, string? Source, DateTime? Uploaded, int? ParentId)
    {
        this.Id = Id;
        this.Name = Name;
        this.CourseTypes = [];
        this.CuisineTypes = [];
        this.Tags = [];
        this.ServingAmount = ServingAmount;
        this.ServingName = ServingName;
        this.Source = Source;
        this.Uploaded = Uploaded;
        this.ParentId = ParentId;
        this.SubRecipes = [];
    }

    public Recipe Clone()
    {
        return new Recipe(this.Id, this.Name, this.ServingAmount, this.ServingName, this.Source, this.Uploaded, this.ParentId)
        {
            CourseTypes = [.. this.CourseTypes],
            CuisineTypes = [.. this.CuisineTypes],
            Tags = [.. this.Tags],
            SubRecipes = [.. this.SubRecipes]
        };
    }
}
