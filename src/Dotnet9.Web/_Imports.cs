global using AutoMapper;
global using Dotnet9.Application.Contracts.Abouts;
global using Dotnet9.Application.Contracts.Albums;
global using Dotnet9.Application.Contracts.Blogs;
global using Dotnet9.Application.Contracts.Caches;
global using Dotnet9.Application.Contracts.Categories;
global using Dotnet9.Application.Contracts.UrlLinks;
global using Dotnet9.Core;
global using Dotnet9.Domain.Abouts;
global using Dotnet9.Domain.Albums;
global using Dotnet9.Domain.Blogs;
global using Dotnet9.Domain.Categories;
global using Dotnet9.Domain.Donations;
global using Dotnet9.Domain.Privacies;
global using Dotnet9.Domain.Repositories;
global using Dotnet9.Domain.Shared.Blogs;
global using Dotnet9.Domain.Tags;
global using Dotnet9.Domain.Timelines;
global using Dotnet9.Domain.UrlLinks;
global using Dotnet9.EntityFrameworkCore.EntityFrameworkCore;
global using Dotnet9.Extensions;
global using Dotnet9.Extensions.CountSystemInfo;
global using Dotnet9.Extensions.Repository;
global using Dotnet9.Web.AutoMapper;
global using Dotnet9.Web.Caches;
global using Dotnet9.Web.Filters;
global using Dotnet9.Web.Models;
global using Dotnet9.Web.Serilog;
global using Dotnet9.Web.ServiceExtensions;
global using Dotnet9.Web.Settings;
global using Dotnet9.Web.Utils;
global using Dotnet9.Web.ViewModels.Abouts;
global using Dotnet9.Web.ViewModels.Blogs;
global using Dotnet9.Web.ViewModels.Homes;
global using Dotnet9.Web.ViewModels.Logins;
global using Microsoft.AspNetCore.Authentication.Cookies;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.HttpOverrides;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Caching.StackExchangeRedis;
global using Microsoft.Extensions.Options;
global using Newtonsoft.Json;
global using Serilog;
global using StackExchange.Redis;
global using System.Diagnostics;
global using System.Linq.Expressions;
global using System.Net;
global using System.Net.Mime;
global using System.Text;
global using System.Text.Encodings.Web;
global using System.Text.RegularExpressions;
global using System.Text.Unicode;
global using Dotnet9.Application.Contracts.Donations;
global using Dotnet9.Web.ViewModels.Donations;
global using Dotnet9.Application.Contracts.Privacies;
global using Dotnet9.Web.ViewModels.Privacies;
global using Dotnet9.Application.Contracts.Tags;
global using Dotnet9.Web.HttpContext;
global using Dotnet9.Web.ViewModels.Accounts;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Dotnet9.Domain.ActionLogs;
global using UAParser;
global using System.ComponentModel.DataAnnotations;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.IdentityModel.Tokens;
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global using Microsoft.AspNetCore.Authentication.JwtBearer;