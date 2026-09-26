using System.Collections.Generic;
using System.Linq;

namespace HuTao.Services.Quote;

/// <summary>One gutter cell of a quote. Each cell is drawn as one connector emoji.</summary>
public enum RailCell
{
    Space,
    Line,
    Tee,
    Top,
    End
}

/// <summary>
///     Decides where every turn of a quote sits and which connectors it gets. Both the collapsed and the expanded
///     quote go through here; the two modes differ only in which blocks they link together beforehand.
/// </summary>
/// <remarks>
///     The rules, in order:
///     <list type="number">
///         <item>The trunk (the root down to the quoted turn) is one straight rail.</item>
///         <item>Replies to the same message share one rail.</item>
///         <item>A turn outside the trunk that has a sibling on its rail puts its own replies on a new rail, one column
///             right, under its text. So a straight chain never moves right, and only a real fork makes a column.</item>
///         <item>The last turn of the main rail (the quoted turn) has no connector: the line runs straight into it,
///             the way Discord draws a reply.</item>
///     </list>
/// </remarks>
public static class QuoteLayout
{
    public abstract record Row(IReadOnlyList<RailCell> Cells);

    /// <summary>A turn. <see cref="Row.Cells" /> go before its header, <paramref name="ContentCells" /> before the rest.</summary>
    public sealed record TurnRow(MessageBlock Block, IReadOnlyList<RailCell> Cells, IReadOnlyList<RailCell> ContentCells)
        : Row(Cells);

    /// <summary>A line with no text, only rails, between a turn and the next one.</summary>
    public sealed record GapRow(IReadOnlyList<RailCell> Cells) : Row(Cells);

    private sealed record Entry(MessageBlock Block, List<Entry>? Nested);

    /// <param name="root">The top of the conversation. Its <see cref="MessageBlock.Children" /> must already be linked.</param>
    /// <param name="trunk">Head ids of the blocks from the root down to the quoted block.</param>
    public static List<Row> Layout(MessageBlock root, IReadOnlySet<ulong> trunk)
    {
        var rows = new List<Row>();
        Draw(Rail(root, trunk, alone: true), [], main: true, rows);
        return rows;
    }

    /// <summary>The turns that sit on one rail, starting at <paramref name="block" />.</summary>
    /// <param name="alone">Whether the block has no sibling on this rail.</param>
    private static List<Entry> Rail(MessageBlock block, IReadOnlySet<ulong> trunk, bool alone)
    {
        var kids = block.Children;
        if (kids.Count > 0 && !alone && !trunk.Contains(block.Head.Id))
            return [new Entry(block, Replies(kids, trunk))];

        var entries = new List<Entry> { new(block, null) };
        if (kids.Count == 1)
            entries.AddRange(Rail(kids[0], trunk, alone: true));
        else
            entries.AddRange(kids.SelectMany(kid => Rail(kid, trunk, alone: false)));

        return entries;
    }

    private static List<Entry> Replies(List<MessageBlock> kids, IReadOnlySet<ulong> trunk)
        => kids.Count == 1
            ? Rail(kids[0], trunk, alone: true)
            : kids.SelectMany(kid => Rail(kid, trunk, alone: false)).ToList();

    private static void Draw(List<Entry> entries, List<RailCell> cols, bool main, List<Row> rows)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            var (block, nested) = entries[i];
            var last = i == entries.Count - 1;

            if (main && last && nested is null)
            {
                rows.Add(new TurnRow(block, cols, cols));
                continue;
            }

            var connector = main && i == 0 ? RailCell.Top : last ? RailCell.End : RailCell.Tee;
            var below = last ? RailCell.Space : RailCell.Line;
            rows.Add(new TurnRow(block, [.. cols, connector], [.. cols, below]));

            if (nested is not null)
            {
                rows.Add(new GapRow([.. cols, below, RailCell.Line]));
                Draw(nested, [.. cols, below], main: false, rows);
            }

            if (!last)
                rows.Add(new GapRow([.. cols, RailCell.Line]));
        }
    }
}
