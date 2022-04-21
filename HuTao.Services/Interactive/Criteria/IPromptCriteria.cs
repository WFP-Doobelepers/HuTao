﻿using System.Collections.Generic;
using Discord;
using Discord.Commands;

namespace HuTao.Services.Interactive.Criteria;

public interface IPromptCriteria
{
    ICollection<ICriterion<IMessage>>? Criteria { get; }

    TypeReader? TypeReader { get; }
}