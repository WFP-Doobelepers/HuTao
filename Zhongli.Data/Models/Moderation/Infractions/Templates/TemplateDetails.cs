using Zhongli.Data.Models.Authorization;

namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public record TemplateDetails(string Name, string? Reason = null,
    AuthorizationScope Scope = AuthorizationScope.Moderator);