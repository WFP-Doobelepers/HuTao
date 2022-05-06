using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules;

[Group("tc", "Theorycrafting related commands")]
public class TheorycraftingModule : InteractionModuleBase<SocketInteractionContext>
{
    public enum Refinement
    {
        R1 = 1,
        R2 = 2,
        R3 = 3,
        R4 = 4,
        R5 = 5
    }

    [SlashCommand("favonius", "Calculate Favonius proc chance")]
    public async Task FavoniusAsync(
        [Summary(description: "crit rate%")][MinValue(0)] double critRate,
        [Summary(description: "number of hits")][MinValue(0)] int hitCount,
        [Summary(description: "refinement level")] Refinement refinementLevel)
    {
        var chance = 1 - Math.Pow(1 - (Math.Min(critRate, 100) / 100) * (.5 + (int) refinementLevel * .1), hitCount);
        var embed = new EmbedBuilder()
            .WithTitle("Favonius Calculator")
            .AddField("Crit Rate", $"{critRate}%", inline: true)
            .AddField("# of Hits", $"{hitCount}", inline: true)
            .AddField("Refinement", $"{refinementLevel}", inline: true)
            .AddField("Chance of procing", $"{chance:P}");

        await RespondAsync(embed: embed.Build(), ephemeral: false);
        return;
    }

    [SlashCommand("crit-compare", "Compare the damage between two crit ratios")]
    public async Task CritCompareAsync(
        [Summary(description: "Build 1 Crit Rate%")][MinValue(0)] double critRate1,
        [Summary(description: "Build 1 Crit Damage%")][MinValue(0)] double critDamage1,
        [Summary(description: "Build 2 Crit Rate%")][MinValue(0)] double critRate2,
        [Summary(description: "Build 2 Crit Damage%")][MinValue(0)] double critDamage2)
    {
        var build1 = critMod(critRate1 / 100, critDamage1 / 100);
        var build2 = critMod(critRate2 / 100, critDamage2 / 100);

        var embed = new EmbedBuilder()
            .WithTitle("Crit Ratio Comparison")
            .AddField($"{critRate1}:{critDamage1}", $"{build1:p} Damage", inline: true)
            .AddField($"{critRate2}:{critDamage2}", $"{build2:p} Damage", inline: true);

        if (build1 > build2)
        {
            embed.AddContent($"{critRate1}:{critDamage1} is {(build1 / build2) - 1:p} stronger");
        }
        else if (build2 > build1)
        {
            embed.AddContent($"{critRate2}:{critDamage2} is {(build2 / build1) - 1:p} stronger");
        }
        else
        {
            embed.AddContent("They do the same damage");
        }

        await RespondAsync(embed: embed.Build(), ephemeral: false);
        return;
    }

    private static double critMod(double critRate, double critDamage) => 1 + Math.Min(critRate, 1) * critDamage;
}

