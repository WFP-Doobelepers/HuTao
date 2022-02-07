using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zhongli.Data.Dtos.Core;

namespace Zhongli.Services.Core.Interfaces;
//namespace Zhongli.Api;

public interface IGenericApiService
{
    Task<IActionResult> GetAsync(GenericRequestDto genericRequestDto);
    Task<IActionResult> PostAsync(GenericRequestDto genericRequestDto);
    Task<IActionResult> PostStartAsync(Guid id, GenericRequestDto genericRequestDto);
    Task<IActionResult> PostCancelAsync(Guid id, GenericRequestDto genericRequestDto);

    Task<IActionResult> PutAsync(GenericRequestDto genericRequestDto);
    Task<IActionResult> DeleteAsync(GenericRequestDto genericRequestDto);
}