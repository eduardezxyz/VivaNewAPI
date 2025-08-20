using AutoMapper;
using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public class AutoMapperConfig : Profile
{
    public AutoMapperConfig()
    {
        // Add your mappings here Example: CreateMap<Source, Destination>();
        CreateMap<ProjectsVw, Project>().ReverseMap();
        CreateMap<SubcontractorsVw, Subcontractor>().ReverseMap();
        CreateMap<GeneralContractorsVw, GeneralContractor>().ReverseMap();
        CreateMap<PayAppsVw, PayApp>().ReverseMap();
        CreateMap<PayAppHistoryVw, PayAppHistory>().ReverseMap();
        CreateMap<SubcontractorProjectsVw, SubcontractorProject>().ReverseMap();
        CreateMap<UserProfilesVw, UserProfile>().ReverseMap();
        CreateMap<DocumentsVw, Document>().ReverseMap();
    }
}

