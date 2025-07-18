using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace Asahi.Modules.AnimeThemes;

public static class AnimeThemesPaginatorGenerator
{
    public const string AnimeChoiceButtonId = "atv3-ac:";
    public const string ThemeChoiceButtonId = "atv3-tc:";
    public const string BackButtonId = "atv3-tc:";
    public const string RefreshVideoId = "atv3-rv:";

    public static IPage GeneratePage(IComponentPaginator paginator, BotConfig config, BotEmoteService emoteService)
    {
        var state = paginator.GetUserState<AnimeThemesSelectionState>();

        return state.CurrentStep switch
        {
            AnimeThemesSelectionState.VideoDisplayState videoDisplayState =>
                GenerateVideoDisplayPage(paginator, videoDisplayState, emoteService),

            AnimeThemesSelectionState.ThemeSelectionState themeSelectionState =>
                GenerateThemeSelectionPage(paginator, themeSelectionState, config),

            _ => GenerateAnimeSelectionPage(paginator, state.CurrentStep)
        };
    }

    private static Page GenerateAnimeSelectionPage(IComponentPaginator p,
        AnimeThemesSelectionState.AnimeSelectionState state)
    {
        var chunk = state.SearchResponse.search.anime.Chunk(AnimeThemesSelectionState.MaxAnimePerPage)
            .ElementAt(p.CurrentPageIndex);
        var container = new ContainerBuilder();

        for (var i = 0; i < chunk.Length; i++)
        {
            var anime = chunk[i];

            var totalThemes = anime.animethemes?.Length ?? 0;

            var titleComponent = new SectionBuilder();
            titleComponent.WithTextDisplay(
                $"### {i + 1}. {anime.name}\n{anime.media_format.GetValueOrDefault()} • {anime.season} {anime.year} • {totalThemes} {(totalThemes == 1 ? "theme" : "themes")}");

            var image = GetAnimeThumbnail(anime);
            var media = new UnfurledMediaItemProperties(image);

            titleComponent.WithAccessory(new ThumbnailBuilder().WithMedia(media));

            // ---

            container.WithSection(titleComponent);

            if (chunk.Length - 1 != i)
            {
                var separator = new SeparatorBuilder().WithIsDivider(true);
                container.WithSeparator(separator);
            }
        }

        container.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
        container.WithActionRow(new ActionRowBuilder().WithComponents(chunk.Select((x, i) =>
            new ButtonBuilder((i + 1).ToString(), $"{AnimeChoiceButtonId}{x.id}", ButtonStyle.Success,
                isDisabled: p.ShouldDisable()))));

        container.WithSeparator(new SeparatorBuilder().WithIsDivider(false).WithSpacing(SeparatorSpacingSize.Small));

        container.WithActionRow(new ActionRowBuilder()
            .AddPreviousButton(p, "<", ButtonStyle.Secondary)
            .AddPageIndicatorButton(p)
            .AddNextButton(p, ">", ButtonStyle.Secondary));

        var components = new ComponentBuilderV2()
            .WithContainer(container);
        var builtComponents = components.Build();

        return new PageBuilder()
            .WithComponents(builtComponents)
            .Build();
    }

    private static Page GenerateThemeSelectionPage(IComponentPaginator p,
        AnimeThemesSelectionState.ThemeSelectionState state, BotConfig config)
    {
        var chunk = state.SelectedAnime.animethemes!.Order(new AnimeThemeResourceComparer())
            .Chunk(AnimeThemesSelectionState.MaxThemesPerPage)
            .ElementAt(p.CurrentPageIndex);

        var container = new ContainerBuilder();

        for (var i = 0; i < chunk.Length; i++)
        {
            var theme = chunk[i];
            Debug.Assert(theme.animeThemeEntries != null);

            var titleText = ThemeToString(theme);
            var titleComponent = new TextDisplayBuilder(titleText);

            var videos = theme.animeThemeEntries.First().videos;

            Debug.Assert(videos != null);

            var thumbnailVideo = AnimeThemesModule.SelectBestVideoSource(videos);
            var thumbnailVideoLink = thumbnailVideo.link;

            Debug.Assert(thumbnailVideoLink != null);

            var titleSectionComponent = new SectionBuilder().WithTextDisplay(titleComponent)
                .WithAccessory(
                    new ThumbnailBuilder(
                        new UnfurledMediaItemProperties(GetAnimeVideoThumbnailUrl(thumbnailVideoLink, config))));

            container.WithSection(titleSectionComponent);

            foreach (var entryChunk in
                     theme.animeThemeEntries.Chunk(4)) // 4 buttons is where discord seems to wrap buttons
            {
                var actionRow = new ActionRowBuilder();

                foreach (var entry in entryChunk)
                {
                    var button = new ButtonBuilder(entry.ToString(),
                        StateSerializer.SerializeObject(new ThemeAndEntrySelection
                            { SelectedEntry = entry.id, SelectedTheme = theme.id }, ThemeChoiceButtonId),
                        ButtonStyle.Success, isDisabled: p.ShouldDisable());

                    actionRow.WithButton(button);
                }

                container.WithActionRow(actionRow);
            }

            var isLastElement = i == chunk.Length - 1;
            if (!isLastElement)
                container.WithSeparator(new SeparatorBuilder().WithIsDivider(true)
                    .WithSpacing(SeparatorSpacingSize.Large));
        }

        container.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
        container.WithActionRow(new ActionRowBuilder()
            .AddPreviousButton(p, "<", ButtonStyle.Secondary)
            .AddPageIndicatorButton(p)
            .AddNextButton(p, ">", ButtonStyle.Secondary)
            .WithButton("Back", BackButtonId, ButtonStyle.Danger, disabled: p.ShouldDisable()));

        var components = new ComponentBuilderV2()
            .WithContainer(container);
        var builtComponents = components.Build();

        return new PageBuilder()
            .WithComponents(builtComponents)
            .Build();
    }

    private static IPage GenerateVideoDisplayPage(IComponentPaginator p,
        AnimeThemesSelectionState.VideoDisplayState state, BotEmoteService emoteService)
    {
        var videoUrl = state.SelectedVideo.link;
        if (state.CacheBustingMeasures)
        {
            videoUrl += $"?cache-bust={Guid.NewGuid()}";
        }

        var videoEmbedComponents = new ComponentBuilderV2().WithComponents([
            new ContainerBuilder().WithComponents([
                new MediaGalleryBuilder([
                    new MediaGalleryItemProperties(new UnfurledMediaItemProperties(videoUrl),
                        isSpoiler: state.SelectedThemeEntry.spoiler.GetValueOrDefault())
                ]),
                new SectionBuilder()
                    .WithComponents([
                        new TextDisplayBuilder(
                            $"{ThemeToString(state.SelectedTheme, $" • {state.SelectedThemeEntry}")}\nfrom *{state.SelectedAnime.name}*")
                    ]).WithAccessory(
                        new ThumbnailBuilder(new UnfurledMediaItemProperties(GetAnimeThumbnail(state.SelectedAnime)))),
                // new SectionBuilder().WithComponents([new TextDisplayBuilder("\u200b")])
                //     .WithAccessory(new ButtonBuilder("Refresh Video", RefreshVideoId, ButtonStyle.Secondary,
                //         emote: emoteService.Refresh, isDisabled: p.ShouldDisable())),
                new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large),
                new ActionRowBuilder().WithComponents([
                    new ButtonBuilder("Back", BackButtonId, ButtonStyle.Danger,
                        isDisabled: p.ShouldDisable()),
                    new ButtonBuilder("Refresh Video", RefreshVideoId, ButtonStyle.Secondary,
                        emote: emoteService.Refresh, isDisabled: p.ShouldDisable()),
                ])
                // new SectionBuilder().WithComponents([new TextDisplayBuilder("\u200b")])
                // .WithAccessory(new ButtonBuilder("Back", BackButtonId, ButtonStyle.Danger,
                // isDisabled: p.ShouldDisable())),
            ])
        ]);

        var builtComponents = videoEmbedComponents.Build();

        return new PageBuilder()
            .WithComponents(builtComponents)
            .Build();
    }

    #region Utility methods

    private static string GetAnimeVideoThumbnailUrl(string url, BotConfig config)
    {
        var base64EncodedUrl = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(url));

        return $"{config.AsahiWebServicesBaseUrl}/api/thumb/{base64EncodedUrl}.png";
    }

    private static string GetAnimeThumbnail(AnimeResource anime)
    {
        return anime.images?.FirstOrDefault(x => x.facet == ImageResource.Facet.SmallCover)?.link ??
               "https://cubari.onk.moe/404.png";
    }

    private static string ThemeToString(AnimeThemeResource theme, string entryInformation = "")
    {
        var songInfo = "";

        if (theme.song != null)
        {
            var artistInfo = "";
            if (theme.song.artists != null && theme.song.artists.Length != 0)
            {
                artistInfo = $"\nby *{theme.song.artists.ToStringNice()}*";
            }

            songInfo = $"**{theme.song.title}**{artistInfo}";
        }

        return $"-# {theme.slug}{entryInformation}\n{songInfo}";
    }

    public struct ThemeAndEntrySelection
    {
        public required int SelectedTheme;
        public required int SelectedEntry;
    }

    #endregion
}

public class AnimeThemesSelectionState(SearchResponse searchResponse)
{
    public const int MaxAnimePerPage = 4;
    public const int MaxThemesPerPage = 3;

    public AnimeSelectionState CurrentStep = new(searchResponse);

    public record AnimeSelectionState(SearchResponse SearchResponse)
    {
        public virtual int TotalPages =>
            (int)Math.Ceiling((double)SearchResponse.search.anime.Length / MaxAnimePerPage);
    }

    public record ThemeSelectionState(SearchResponse SearchResponse, AnimeResource SelectedAnime)
        : AnimeSelectionState(SearchResponse)
    {
        public override int TotalPages =>
            (int)Math.Ceiling((double)SelectedAnime!.animethemes!.Length / MaxThemesPerPage);
    }

    public record VideoDisplayState(
        SearchResponse SearchResponse,
        AnimeResource SelectedAnime,
        AnimeThemeResource SelectedTheme,
        AnimeThemeEntryResource SelectedThemeEntry,
        VideoResource SelectedVideo) : ThemeSelectionState(SearchResponse, SelectedAnime)
    {
        public override int TotalPages => 1;
        public bool CacheBustingMeasures = false;
    }
}