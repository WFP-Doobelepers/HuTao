using System;
using System.Threading.Tasks;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Zhongli.Data.Dtos.Core;
using Zhongli.Services.Core.Interfaces;

namespace Zhongli.Services.Core;

public class GenericApiService : IGenericApiService
{
    public GenericApiService(IMapper mapper)
    {

    }

    public Task<IActionResult> GetAsync(GenericRequestDto genericRequestDto)
    {
        throw new System.NotImplementedException();
    }

    public Task<IActionResult> PostAsync(GenericRequestDto genericRequestDto)
    {
        throw new System.NotImplementedException();
    }

    public Task<IActionResult> PostStartAsync(Guid id, GenericRequestDto genericRequestDto)
    {
        throw new NotImplementedException();
    }

    public Task<IActionResult> PostCancelAsync(Guid id, GenericRequestDto genericRequestDto)
    {
        throw new NotImplementedException();
    }
    public Task<IActionResult> PutAsync(GenericRequestDto genericRequestDto)
    {
        throw new System.NotImplementedException();
    }

    public Task<IActionResult> DeleteAsync(GenericRequestDto genericRequestDto)
    {
        throw new System.NotImplementedException();
    }
}