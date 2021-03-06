namespace Dotnet9.Web.Controllers;

public class HomeController : Controller
{
    private readonly IAlbumAppService _albumAppService;
    private readonly AlbumManager _albumManager;
    private readonly IBlogPostAppService _blogPostAppService;
    private readonly BlogPostManager _blogPostManager;
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICacheService _cacheService;
    private readonly ICategoryAppService _categoryAppService;
    private readonly CategoryManager _categoryManager;
    private readonly Dotnet9DbContext _dotnet9DbContext;
    private readonly ILogger<HomeController> _logger;
    private readonly IMapper _mapper;
    private readonly IOptionsSnapshot<SiteSettings> _optSiteSettings;
    private readonly TagManager _tagManager;
    private readonly ITagRepository _tagRepository;
    private readonly UrlLinkManager _urlLinkManager;

    public HomeController(
        ILogger<HomeController> logger,
        IOptionsSnapshot<SiteSettings> optSiteSettings,
        Dotnet9DbContext dotnet9DbContext,
        IAlbumAppService albumAppService,
        AlbumManager albumManager,
        ICategoryAppService categoryAppService,
        CategoryManager categoryManager,
        ITagRepository tagRepository,
        TagManager tagManager,
        IBlogPostAppService blogPostAppService,
        IBlogPostRepository blogPostRepository,
        BlogPostManager blogPostManager,
        UrlLinkManager urlLinkManager,
        ICacheService cacheService,
        IMapper mapper)
    {
        _logger = logger;
        _optSiteSettings = optSiteSettings;
        _dotnet9DbContext = dotnet9DbContext;
        _albumAppService = albumAppService;
        _albumManager = albumManager;
        _categoryAppService = categoryAppService;
        _categoryManager = categoryManager;
        _tagRepository = tagRepository;
        _tagManager = tagManager;
        _blogPostAppService = blogPostAppService;
        _blogPostRepository = blogPostRepository;
        _blogPostManager = blogPostManager;
        _urlLinkManager = urlLinkManager;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        const string cacheKey = $"{nameof(HomeController)}-{nameof(Index)}";
        var cacheData = await _cacheService.GetAsync<HomeViewModel>(cacheKey);
        if (cacheData != null) return View(cacheData);

        cacheData = new HomeViewModel();
        var recommend = await _blogPostRepository.SelectBlogPostBriefAsync(8, 1, x => x.InBanner, x => x.CreateDate,
            SortDirectionKind.Descending);
        cacheData.BlogPostsForRecommend =
            _mapper.Map<List<BlogPostBrief>, List<BlogPostBriefDto>>(recommend.Item1);
        cacheData.LoadMoreKinds = new Dictionary<string, LoadMoreKind>
        {
            { "最新", LoadMoreKind.Latest },
            { ".NET", LoadMoreKind.Dotnet },
            { "大前端", LoadMoreKind.Front },
            { "数据库", LoadMoreKind.Database },
            { "更多语言", LoadMoreKind.MoreLanguage },
            { "课程", LoadMoreKind.Course },
            { "其他", LoadMoreKind.Other }
        };

        await _cacheService.ReplaceAsync(cacheKey, cacheData, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));

        return View(cacheData);
    }

    [HttpGet]
    [Route("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        const string contentType = "application/xml";
        const string cacheKey = "sitemap.xml";
        var cd = new ContentDisposition
        {
            FileName = cacheKey,
            Inline = true
        };
        Response.Headers.Append("Content-Disposition", cd.ToString());

        var bytes = await _cacheService.GetAsync<byte[]>(cacheKey);
        if (bytes is { Length: > 0 }) return File(bytes, contentType);

        var siteMapNodes = new List<SitemapNode>();

        siteMapNodes.AddRange((await _albumAppService.GetListCountAsync()).Select(x => new SitemapNode
        {
            LastModified = DateTimeOffset.UtcNow,
            Priority = 0.8,
            Url = $"{_optSiteSettings.Value.Domain}/album/{x.Slug}",
            Frequency = SitemapFrequency.Monthly
        }));

        siteMapNodes.AddRange((await _categoryAppService.GetListCountAsync()).Select(x => new SitemapNode
        {
            LastModified = DateTimeOffset.UtcNow,
            Priority = 0.8,
            Url = $"{_optSiteSettings.Value.Domain}/cat/{x.Slug}",
            Frequency = SitemapFrequency.Monthly
        }));

        siteMapNodes.AddRange((await GetAllBlogPostForSitemap()).Select(x =>
            new SitemapNode
            {
                LastModified = x.CreateDate,
                Priority = 0.9,
                Url =
                    $"{_optSiteSettings.Value.Domain}/{x.CreateDate.ToString("yyyy")}/{x.CreateDate.ToString("MM")}/{x.Slug}",
                Frequency = SitemapFrequency.Daily
            }));

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"");
        sb.AppendLine("   xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        sb.AppendLine(
            "   xsi:schemaLocation=\"http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd\">");

        foreach (var m in siteMapNodes)
        {
            sb.AppendLine("    <url>");

            sb.AppendLine($"        <loc>{m.Url}</loc>");
            sb.AppendLine($"        <lastmod>{m.LastModified.ToString("yyyy-MM-dd")}</lastmod>");
            sb.AppendLine($"        <changefreq>{m.Frequency}</changefreq>");
            sb.AppendLine($"        <priority>{m.Priority}</priority>");

            sb.AppendLine("    </url>");
        }

        sb.AppendLine("</urlset>");

        bytes = Encoding.UTF8.GetBytes(sb.ToString());

        await _cacheService.AddAsync(cacheKey, bytes, TimeSpan.FromHours(24));
        return File(bytes, contentType);
    }

    [HttpGet]
    [Route("archive")]
    public async Task<IActionResult> Archive()
    {
        var vm = new ArchiveViewModel
        {
            Items = await GetAllBlogPostForSitemap()
        };

        return View(vm);
    }

    public async Task<List<BlogPostForSitemap>?> GetAllBlogPostForSitemap()
    {
        var cacheData = await _cacheService.GetOrCreateAsync(
            $"{nameof(BlogPostController)}-{nameof(GetAllBlogPostForSitemap)}",
            async () => await _blogPostAppService.GetListBlogPostForSitemapAsync());
        return cacheData;
    }

    [HttpGet]
    [Route("seed")]
    public async Task<bool> Seed()
    {
        await SeedAlbums();

        await SeedCategory();

        await SeedBlogPost();

        await SeedUrlLink();

        await SeedAbout();

        await SeedDonation();

        await SeedTimeline();

        await SeedPrivacy();

        return true;
    }

    private async Task SeedPrivacy()
    {
        _logger.LogInformation("做隐私数据种子");
        var addCount = 0;
        if (await _dotnet9DbContext.Privacies!.CountAsync() <= 0)
        {
            var filePath = Path.Combine(_optSiteSettings.Value.AssetsLocalPath!, "site", "Privacy.md");
            if (System.IO.File.Exists(filePath))
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var privacy = new Privacy { Content = fileContent };
                await _dotnet9DbContext.Privacies!.AddAsync(privacy);
                await _dotnet9DbContext.SaveChangesAsync();
                addCount = 1;
            }
        }

        _logger.LogInformation(addCount > 0 ? "做隐私数据种子-----成功" : "做隐私数据种子-----有数据，无须添加");
    }

    private async Task SeedTimeline()
    {
        _logger.LogInformation("做时间线数据种子");
        var addCount = 0;
        if (await _dotnet9DbContext.Timelines!.CountAsync() <= 0)
        {
            var filePath = Path.Combine(_optSiteSettings.Value.AssetsLocalPath!, "site", "timelines.json");
            if (System.IO.File.Exists(filePath))
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var timelinesFromFile = JsonConvert.DeserializeObject<List<Timeline>>(fileContent)!;
                if (timelinesFromFile != null && timelinesFromFile.Any())
                {
                    await _dotnet9DbContext.Timelines!.AddRangeAsync(timelinesFromFile);
                    addCount = await _dotnet9DbContext.SaveChangesAsync();
                }
            }
        }

        _logger.LogInformation(addCount > 0 ? $"做时间线数据种子-----成功添加{addCount}条" : "做时间线数据种子-----有数据，无须添加");
    }

    private async Task SeedDonation()
    {
        _logger.LogInformation("做赞助数据种子");
        var addCount = 0;
        if (await _dotnet9DbContext.Donations!.CountAsync() <= 0)
        {
            var filePath = Path.Combine(_optSiteSettings.Value.AssetsLocalPath!, "pays", "Donation.md");
            if (System.IO.File.Exists(filePath))
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var donation = new Donation { Content = fileContent };
                await _dotnet9DbContext.Donations!.AddAsync(donation);
                await _dotnet9DbContext.SaveChangesAsync();
                addCount = 1;
            }
        }

        _logger.LogInformation(addCount > 0 ? "做赞助数据种子-----成功" : "做赞助数据种子-----有数据，无须添加");
    }

    private async Task SeedAbout()
    {
        _logger.LogInformation("做关于数据种子");
        var addCount = 0;
        if (await _dotnet9DbContext.Abouts!.CountAsync() <= 0)
        {
            var filePath = Path.Combine(_optSiteSettings.Value.AssetsLocalPath!, "site", "about.md");
            if (System.IO.File.Exists(filePath))
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var about = new About { Content = fileContent };
                await _dotnet9DbContext.Abouts!.AddAsync(about);
                await _dotnet9DbContext.SaveChangesAsync();
                addCount = 1;
            }
        }

        _logger.LogInformation(addCount > 0 ? "做关于数据种子-----成功" : "做关于数据种子-----有数据，无须添加");
    }

    private async Task SeedUrlLink()
    {
        _logger.LogInformation("做链接数据种子");
        var addCount = 0;
        if (await _dotnet9DbContext.UrlLinks!.CountAsync() <= 0)
        {
            var filePath = Path.Combine(_optSiteSettings.Value.AssetsLocalPath!, "site", "link.json");
            if (System.IO.File.Exists(filePath))
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var urlLinksFromFile = JsonConvert.DeserializeObject<List<UrlLinkSeed>>(fileContent)!;
                var i = 1;
                var urlLinks = urlLinksFromFile?.Select(x =>
                        _urlLinkManager.CreateAsync(i++, x.Index,
                            (UrlLinkKind)Enum.Parse(typeof(UrlLinkKind), x.Kind),
                            x.Name, x.Description, x.Url).Result)
                    .ToList();
                if (urlLinks != null && urlLinks.Any())
                {
                    await _dotnet9DbContext.UrlLinks!.AddRangeAsync(urlLinks);
                    addCount = await _dotnet9DbContext.SaveChangesAsync();
                }
            }
        }


        _logger.LogInformation(addCount > 0 ? $"做链接数据种子-----成功添加{addCount}条" : "做链接数据种子-----有数据，无须添加");
    }

    private async Task SeedBlogPost()
    {
        _logger.LogInformation("做博文数据种子");
        var addCount = 0;
        var errCount = 0;
        if (await _dotnet9DbContext.BlogPosts!.CountAsync() <= 0)
        {
            var blogPostFiles = Directory.GetFiles(_optSiteSettings.Value.AssetsLocalPath!, "*.info",
                SearchOption.AllDirectories);
            var blogPostCount = blogPostFiles.Length;
            var index = 0;
            foreach (var blogPostFile in blogPostFiles)
            {
                _logger.LogInformation($"添加博文{++index}/{blogPostCount}");
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
                    foreach (var tagName in blogPostSeed.Tags)
                        try
                        {
                            var existTag = await _tagRepository.FindByNameAsync(tagName);
                            if (existTag != null) continue;

                            existTag = await _tagManager.CreateAsync(null, tagName);
                            await _dotnet9DbContext.Tags!.AddAsync(existTag);
                            _logger.LogInformation($"添加标签{tagName}");

                            await _dotnet9DbContext.SaveChangesAsync();
                            _logger.LogInformation("提交标签保存");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"保存标签异常：{ex}", ex);
                            // ignored
                        }

                try
                {
                    _logger.LogInformation($"添加博文入库开始-{blogPostSeed.Title}");
                    await _dotnet9DbContext.BlogPosts!.AddAsync(await _blogPostManager.CreateAsync(
                        blogPostSeed.Title,
                        blogPostSeed.Slug,
                        blogPostSeed.BriefDescription,
                        blogPostSeed.InBanner,
                        blogPostSeed.Cover,
                        blogPostSeed.Content,
                        blogPostSeed.CopyrightType!.Value,
                        blogPostSeed.Original,
                        null,
                        blogPostSeed.OriginalTitle,
                        blogPostSeed.OriginalLink,
                        blogPostSeed.Albums,
                        blogPostSeed.Categories,
                        blogPostSeed.Tags,
                        DateTimeOffset.Parse(blogPostSeed.CreateDate!)));
                    await _dotnet9DbContext.SaveChangesAsync();
                    _logger.LogInformation($"添加博文入库成功-{blogPostSeed.Title}");

                    addCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"做BlogPost种子异常：{ex}", ex);
                    errCount++;
                }
            }
        }


        _logger.LogInformation(addCount > 0 ? $"做博文数据种子-----成功添加{addCount}条，失败{errCount}条" : "做博文数据种子-----有数据，无须添加");
    }

    private async Task SeedCategory()
    {
        _logger.LogInformation("做分类数据种子");
        var addCount = 0;
        if (await _dotnet9DbContext.Categories!.CountAsync() <= 0)
        {
            var filePath = Path.Combine(_optSiteSettings.Value.AssetsLocalPath!, "cats", "category.json");
            if (System.IO.File.Exists(filePath))
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var categoriesFromFile = JsonConvert.DeserializeObject<List<CategoryItem>>(fileContent)!;
                var i = 1;
                var categoriesToDb = new List<Category>();
                foreach (var child in categoriesFromFile)
                {
                    ReadCategory(categoriesToDb, child, ref i, -1);
                    i++;
                }

                if (categoriesToDb.Any())
                {
                    await _dotnet9DbContext.Categories!.AddRangeAsync(categoriesToDb);
                    addCount = await _dotnet9DbContext.SaveChangesAsync();
                }
            }
        }


        _logger.LogInformation(addCount > 0 ? $"做分类数据种子-----成功添加{addCount}条" : "做分类数据种子-----有数据，无须添加");
    }

    private async Task SeedAlbums()
    {
        _logger.LogInformation("做专辑数据种子");
        var addCount = 0;
        if (await _dotnet9DbContext.Albums!.CountAsync() <= 0)
        {
            var filePath = Path.Combine(_optSiteSettings.Value.AssetsLocalPath!, "albums", "album.json");
            if (System.IO.File.Exists(filePath))
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var albumsFromFile = JsonConvert.DeserializeObject<List<AlbumItem>>(fileContent);
                var i = 1;
                var albums = albumsFromFile?.Select(x => _albumManager.CreateAsync(i++, x.Name, x.Slug,
                        Path.Combine(_optSiteSettings.Value.AssetsRemotePath!, x.Cover), null, -1).Result
                    )
                    .ToList();
                if (albums != null && albums.Any())
                {
                    await _dotnet9DbContext.Albums!.AddRangeAsync(albums);
                    addCount = await _dotnet9DbContext.SaveChangesAsync();
                }
            }
        }


        _logger.LogInformation(addCount > 0 ? $"做专辑数据种子-----成功添加{addCount}条" : "做专辑数据种子-----有数据，无须添加");
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void ReadCategory(List<Category> container, CategoryItem categoryFromFile, ref int id,
        int parentId)
    {
        var currentId = id;
        var category = _categoryManager.CreateAsync(currentId, categoryFromFile.Name, categoryFromFile.Slug,
            Path.Combine(_optSiteSettings.Value.AssetsRemotePath!, categoryFromFile.Cover), null, parentId).Result;
        container.Add(category);

        if (categoryFromFile.Children is not { Count: > 0 }) return;
        foreach (var child in categoryFromFile.Children)
        {
            id++;
            ReadCategory(container, child, ref id, currentId);
        }
    }
}