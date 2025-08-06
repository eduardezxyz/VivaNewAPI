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
        CreateMap<SubcontractorsVw, Subcontractor>().ReverseMap();
        CreateMap<GeneralContractorsVw, GeneralContractor>().ReverseMap();
        // Example: CreateMap<Source, Destination>();
    }
}

