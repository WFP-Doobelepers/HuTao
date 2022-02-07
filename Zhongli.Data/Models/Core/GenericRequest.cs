using System;
using Zhongli.Data.Models.Core.Enums;

namespace Zhongli.Data.Models.Core;

public class GenericRequest
{
    Guid? Id { get; set; }
    DateTime? UpdateTime { get; set; }
    string? Content { get; set; }
    DevAuth? DevAuth { get; set; }
}