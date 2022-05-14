using System.ComponentModel;

namespace HuTao.Data.Models.Authorization;

public enum JudgeType
{
    [Description("Pass as long as one of the criteria is met.")]
    Any,

    [Description("Pass only if all of the criteria are met.")]
    All
}