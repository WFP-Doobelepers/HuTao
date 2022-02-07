using Microsoft.AspNetCore.Mvc;
using Zhongli.Services.Core.Interfaces;
using Zhongli.Data.Dtos.Core;

namespace Zhongli.Api;

[ApiController]
[Route("api/zhongli/")]
public class GenericApiController : ControllerBase
{
    private readonly IGenericApiService _genericService;

    public GenericApiController(IGenericApiService genericService)
    {
        this._genericService = genericService;
    }

    [HttpPost("post")]
    [ProducesResponseType(typeof(GenericRequestDto), 200)]
    public async Task<IActionResult> PostAsync([FromBody] GenericRequestDto input)
    {
        var response = await _genericService.PostAsync(input);
        return Ok(response);
    }

    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(GenericRequestDto), 200)]
    public async Task<IActionResult> PostStartAsync(Guid id, [FromBody] GenericRequestDto input)
    {
        var response = await _genericService.PostStartAsync(id, input);
        return Ok(response);
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(GenericRequestDto), 200)]
    public async Task<IActionResult> PostCancelAsync(Guid id, [FromBody] GenericRequestDto input)
    {
        var response = await _genericService.PostCancelAsync(id, input);
        return Ok(response);
    }
    [HttpGet("get")]
    [ProducesResponseType(typeof(GenericRequestDto), 200)]
    public async Task<IActionResult> GetAsync([FromBody] GenericRequestDto input)
    {
        var response = await _genericService.GetAsync(input);
        return Ok(response);
    }

    [HttpDelete("delete")]
    [ProducesResponseType(typeof(GenericRequestDto), 200)]
    public async Task<IActionResult> DeleteAsync([FromBody] GenericRequestDto input)
    {
        var response = await _genericService.DeleteAsync(input);
        return Ok(response);
    }

    [HttpPut("put")]
    [ProducesResponseType(typeof(GenericRequestDto), 200)]
    public async Task<IActionResult> PutAsync([FromBody] GenericRequestDto input)
    {
        var response = await _genericService.PutAsync(input);
        return Ok(response);
    }

}