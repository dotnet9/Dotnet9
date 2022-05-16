﻿using Dotnet9.Application.Contracts.Abouts;
using Dotnet9.Application.Contracts.Caches;
using Dotnet9.Web.ViewModels.Abouts;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace Dotnet9.Web.Controllers;

public class AboutController : Controller
{
    private readonly IAboutAppService _aboutAppService;
    private readonly ICacheService _cacheService;

    public AboutController(IAboutAppService aboutAppService, ICacheService cacheService)
    {
        _aboutAppService = aboutAppService;
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        const string cacheKey = $"{nameof(AboutController)}-{nameof(Index)}";
        var cacheData = await _cacheService.GetAsync<AboutViewModel>(cacheKey);
        if (cacheData != null) return View(cacheData);

        cacheData = new AboutViewModel
        {
            About = await _aboutAppService.GetAsync()
        };
        await _cacheService.ReplaceAsync(cacheKey, cacheData, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));

        return View(cacheData);
    }
}