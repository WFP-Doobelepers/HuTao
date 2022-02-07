using System;
using Zhongli.Data.Models.Core.Enums;

namespace Zhongli.Data.Dtos.Core;

public class GenericRequestDto
{
    Guid? Id { get; set; }
    DateTime? UpdateTime { get; set; }
    string? Content { get; set; }
    DevAuthDto? DevAuthDto { get; set; }

}