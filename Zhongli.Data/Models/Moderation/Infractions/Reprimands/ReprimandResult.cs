namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public record ReprimandResult(ReprimandAction? Reprimand);

    public record WarningResult(Warning? Warning, ReprimandAction? Reprimand = null) : ReprimandResult(Reprimand);

    public record NoticeResult(Notice Notice, WarningResult? Result = null) : WarningResult(Result?.Warning,
        Result?.Reprimand);
}