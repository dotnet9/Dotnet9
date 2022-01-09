﻿using AutoMapper;
using Dotnet9.Abouts;
using Dotnet9.Albums;
using Dotnet9.Tags;

namespace Dotnet9;

public class Dotnet9ApplicationAutoMapperProfile : Profile
{
    public Dotnet9ApplicationAutoMapperProfile()
    {
        CreateMap<Tag, TagDto>();
        CreateMap<CreateTagDto, Tag>();
        CreateMap<Album, AlbumDto>();
        CreateMap<CreateAlbumDto, Album>();
        CreateMap<About, AboutDto>();
        CreateMap<UpdateAboutDto, About>();
    }
}