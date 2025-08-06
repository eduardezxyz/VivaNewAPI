using AutoMapper;
using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public class AutoMapperConfig : Profile
{
    public AutoMapperConfig()
    {
        // Add your mappings here
        CreateMap<ProjectsVw, Project>().ReverseMap();
        // Example: CreateMap<Source, Destination>();
    }
}

