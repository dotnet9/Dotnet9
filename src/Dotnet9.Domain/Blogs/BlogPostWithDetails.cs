using Dotnet9.Domain.Albums;
using Dotnet9.Domain.Categories;
using Dotnet9.Domain.Shared.Blogs;

namespace Dotnet9.Domain.Blogs;

public class BlogPostWithDetails
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? BriefDescription { get; set; }

    public bool InBanner { get; set; }

    public string Content { get; set; } = null!;

    public string Cover { get; set; } = null!;

    public CopyrightType CopyrightType { get; set; }

    public string? Original { get; set; }

    public string? OriginalTitle { get; set; }

    public string? OriginalLink { get; set; }

    public AlbumBrief[]? AlbumNames { get; set; }

    public CategoryBrief[]? CategoryNames { get; set; }

    public string[]? TagNames { get; set; }

    public DateTimeOffset? CreateDate { get; set; }
}