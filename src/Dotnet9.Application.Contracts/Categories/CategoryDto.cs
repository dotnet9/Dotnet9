namespace Dotnet9.Application.Contracts.Categories;

public class CategoryDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Cover { get; set; } = null!;
    public string? Description { get; set; }
    public int Index { get; set; }
    public bool IsShow { get; set; }
}