using Dotnet9.Application.Contracts.Blogs;

namespace Dotnet9.Application.Contracts.Categories;

public class CategoryViewModel
{
    public string Name { get; set; } = null!;
    public List<BlogPostBriefDto>? BlogPosts { get; set; }
    public List<CategoryCountDto>? Categories { get; set; }
}