namespace HuTao.Data.Models.Moderation.Infractions;

public interface IAction
{
    string Action { get; }

    string CleanAction { get; }
}