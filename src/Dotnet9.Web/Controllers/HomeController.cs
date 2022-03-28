﻿using System.Diagnostics;
using Dotnet9.Core;
using Dotnet9.Domain.Albums;
using Dotnet9.Domain.Blogs;
using Dotnet9.Domain.Categories;
using Dotnet9.Domain.Shared.Blogs;
using Dotnet9.Domain.Tags;
using Dotnet9.EntityFrameworkCore.EntityFrameworkCore;
using Dotnet9.Web.Models;
using Dotnet9.Web.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Dotnet9.Web.Controllers;

public class HomeController : Controller
{
    private readonly AlbumManager _albumManager;
    private readonly IAlbumRepository _albumRepository;
    private readonly CategoryManager _categoryManager;
    private readonly ITagRepository _tagRepository;
    private readonly TagManager _tagManager;
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly BlogPostManager _blogPostManager;
    private readonly ICategoryRepository _categoryRepository;
    private readonly Dotnet9DbContext _Dotnet9DbContext;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ILogger<HomeController> logger,
        Dotnet9DbContext Dotnet9DbContext,
        IAlbumRepository albumRepository,
        AlbumManager albumManager,
        ICategoryRepository categoryRepository,
        CategoryManager categoryManager,
        ITagRepository tagRepository,
        TagManager tagManager,
        IBlogPostRepository blogPostRepository,
        BlogPostManager blogPostManager)
    {
        _logger = logger;
        _Dotnet9DbContext = Dotnet9DbContext;
        _albumRepository = albumRepository;
        _albumManager = albumManager;
        _categoryRepository = categoryRepository;
        _categoryManager = categoryManager;
        _tagRepository = tagRepository;
        _tagManager = tagManager;
        _blogPostRepository = blogPostRepository;
        _blogPostManager = blogPostManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }


    public async Task<bool> Seed()
    {
        if (await _Dotnet9DbContext.Albums!.CountAsync() <= 0)
        {
            var albumJsonFilePath = Path.Combine(GlobalVar.AssetsLocalPath!, "albums", "album.json");
            if (System.IO.File.Exists(albumJsonFilePath))
            {
                var albumJsonString = await System.IO.File.ReadAllTextAsync(albumJsonFilePath);
                var albumsFromFile = JsonConvert.DeserializeObject<List<AlbumItem>>(albumJsonString);
                var i = 1;
                var albums = albumsFromFile?.Select(x => _albumManager.CreateAsync(i++, x.Name, x.Slug,
                        Path.Combine(GlobalVar.AssetsRemotePath!, x.Cover), null).Result
                    )
                    .ToList();
                if (albums != null && albums.Any())
                {
                    await _Dotnet9DbContext.Albums!.AddRangeAsync(albums);
                    await _Dotnet9DbContext.SaveChangesAsync();
                }
            }
        }

        if (await _Dotnet9DbContext.Categories!.CountAsync() <= 0)
        {
            var categoryJsonFilePath = Path.Combine(GlobalVar.AssetsLocalPath!, "cats", "category.json");
            if (System.IO.File.Exists(categoryJsonFilePath))
            {
                var categoryJsonString = await System.IO.File.ReadAllTextAsync(categoryJsonFilePath);
                var categoriesFromFile = JsonConvert.DeserializeObject<List<CategoryItem>>(categoryJsonString)!;
                var i = 1;
                var categoriesToDb = new List<Category>();
                foreach (var child in categoriesFromFile)
                {
                    ReadCategory(categoriesToDb, child, ref i, -1);
                    i++;
                }

                if (categoriesToDb.Any())
                {
                    await _Dotnet9DbContext.Categories!.AddRangeAsync(categoriesToDb);
                    await _Dotnet9DbContext.SaveChangesAsync();
                }
            }
        }

        if (await _Dotnet9DbContext.BlogPosts!.CountAsync() <= 0)
        {
            var blogPostFiles = Directory.GetFiles(GlobalVar.AssetsLocalPath!, "*.info", SearchOption.AllDirectories);
            foreach (var blogPostFile in blogPostFiles)
            {
                var blogPostSeed =
                    JsonConvert.DeserializeObject<BlogPostItem>(await System.IO.File.ReadAllTextAsync(blogPostFile))!;
                blogPostSeed.Content = await System.IO.File.ReadAllTextAsync(blogPostFile.Replace(".info", ".md"));
                if (blogPostSeed.BriefDescription.IsNullOrWhiteSpace())
                {
                    if (blogPostSeed.Content.Length < BlogPostConsts.MaxBriefDescriptionLength)
                        blogPostSeed.BriefDescription = blogPostSeed.Content;
                    else
                        blogPostSeed.BriefDescription =
                            blogPostSeed.Content.Substring(0, BlogPostConsts.MaxBriefDescriptionLength - 5) + "...";
                }

                if (blogPostSeed.Tags != null && blogPostSeed.Tags.Any())
                {
                    foreach (var tagName in blogPostSeed.Tags)
                    {
                        try
                        {
                            var existTag = await _tagRepository.FindByNameAsync(tagName);
                            if (existTag != null)
                            {
                                continue;
                            }

                            existTag = await _tagManager.CreateAsync(null, tagName);
                            await _Dotnet9DbContext.Tags!.AddAsync(existTag);
                            await _Dotnet9DbContext.SaveChangesAsync();
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }

                try
                {
                    await _Dotnet9DbContext.BlogPosts!.AddAsync(await _blogPostManager.CreateAsync(
                        blogPostSeed.Title,
                        blogPostSeed.Slug,
                        blogPostSeed.BriefDescription,
                        blogPostSeed.Cover,
                        blogPostSeed.Content,
                        blogPostSeed.CopyrightType!.Value,
                        original: blogPostSeed.Original,
                        originalAvatar: null,
                        originalTitle: blogPostSeed.OriginalTitle,
                        originalLink: blogPostSeed.OriginalLink,
                        albumNames: blogPostSeed.Albums,
                        categoryNames: blogPostSeed.Categories,
                        tagNames: blogPostSeed.Tags,
                        createDate: DateTime.Parse(blogPostSeed.CreateDate!)));
                    await _Dotnet9DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"做BlogPost种子异常：{ex}");
                }
            }
        }

        return true;
    }

    private void ReadCategory(List<Category> container, CategoryItem categoryFromFile, ref int id,
        int parentId)
    {
        var currentId = id;
        var category = _categoryManager.CreateAsync(currentId, categoryFromFile.Name, categoryFromFile.Slug,
            Path.Combine(GlobalVar.AssetsRemotePath!, categoryFromFile.Cover), null, parentId).Result;
        container.Add(category);

        if (categoryFromFile.Children is not {Count: > 0}) return;
        foreach (var child in categoryFromFile.Children)
        {
            id++;
            ReadCategory(container, child, ref id, currentId);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}