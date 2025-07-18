So basically
We get 
## 7 new components

- Section
- TextDisplay
- Thumbnail
- MediaGallery
- File
- Separator
- Container
all of these except the thumbnail can be at the top tevel

We also get a `UnfurledMediaItem`, currently only holding the url of an media item and a `MediaGalleryItem`
```cs
public readonly struct UnfurledMediaItem
{
    public string Url { get; }
}

public readonly struct MediaGalleryItem
{
    public UnfurledMediaItem Media { get; }
    public string Description { get; }
    public bool IsSpoiler { get; }
}
```

-# and now some code :3
```cs
public class SectionComponent : IMessageComponent
{   ...
    // TextDisplayComponent only
    public IReadOnlyCollection<IMessageComponent> Components { get; }
    // can hold a thumbnail OR a button
    public IMessageComponent Accessory { get; }
}

public class TextDisplayComponent : IMessageComponent
{   ...
    // up to 4k characters 
    // W
    public string Content { get; }
}

public class ThumbnailComponent : IMessageComponent
{   ...
    public UnfurledMediaItem Media { get; }
    public string Description { get; }
    public bool IsSpoiler { get; }
}

public class MediaGalleryComponent : IMessageComponent
{   ...
    public IReadOnlyCollection<MediaGalleryItem> Items { get; }
}

public class FileComponent : IMessageComponent
{   ...
    // only supports "attachment://" urls
    public UnfurledMediaItem File { get; }
    public bool? IsSpoiler { get; }
}

public enum SeparatorSpacingSize
{
    Small = 1,
    Large = 2
}

public class SeparatorComponent : IMessageComponent
{
    public bool? IsDivider { get; }
    public SeparatorSpacingSize? Spacing { get; }
}

public class ContainerComponent : IMessageComponent
{
    // can hold ActionRowComponent, TextDisplayComponent, SectionComponent, MediaGalleryComponent, SeparatorComponent and FileComponent
    public IReadOnlyCollection<IMessageComponent> Components { get; }
    public uint? AccentColor { get; }
    public bool? IsSpoiler { get; }
}
```

Sending these new components requires the `ComponentsV2 = 1 << 15,` message flag to be set. DNet will do it automagically in most cases, except maybe if you want to send 10 action rows.
Setting this flag also means you can't set `Content` nor `Embed` in the message.

## Limits
-# there's no complete docs on limits still, here's what I remember out of my head
- 4000 characters in all `TextDisplayComponent`s combined
- 1024 characters - max `ThumbnailComponent` description
- 256 characters - max `MediaGalleryItem` description
~~- up to 10 top level components~~
- up to ~~30~~ 40 components total in a message
- max `SectionComponent` children = 3
- max `MediaGalleryComponent` items = 10
~~- max `ContainerComponent` children = 10~~
-# I will update this list as I work on dnet impl/docs get released


There is no solid ETA on the release date *yet*. Rough estimations is that it comes in a month or two.

P.S. no updates for modals still <a:copium_truck1:1113884682170417282><a:copium_truck2:1113884684187873280><a:copium_truck3:1113884687715283085> 

P.P.S. This is NOT a 1:1 replacement to embeds; it's missing stuff like arbitrary images for `author` / `footer` and most noticeable - `Field`s are not a thing in these. Embeds **ARE NOT** getting removed, this is just an alternative to them, **not a replacement**