var components = new ComponentBuilderV2()
    .WithContainer(new ContainerBuilder()
        .WithMediaGallery(["https://cdn.discordapp.com/attachments/964253122547552349/1336440069892083712/7Q3S.gif?ex=67a3d04e&is=67a27ece&hm=059c9d28466f43a50c4b450ca26fc01298a2080356421d8524384bf67ea8f3ab&"])
        .WithTextDisplay("""
                         # Yippie
                         This container is built using some very fancy (not rly) abstraction and extension methods.
                         """)
        .WithSection([
            new TextDisplayBuilder("Some random text is a section")
            ], new ButtonBuilder("Button in a section", "random-button-id-0", style: ButtonStyle.Success))
        .WithSeparator()
        .WithSection([
                new TextDisplayBuilder("""
                                       And another section with a totally-not-a-rickroll [link](https://www.youtube.com/watch?v=dQw4w9WgXcQ)
                                       -# *and a thumbnail from attachments*
                                       """)
            ],
            new ThumbnailBuilder("attachment://wires.png"))
        .WithSeparator(isDivider: false)
        .WithAccentColor(0xff00)
        .WithActionRow([
            ButtonBuilder.CreateLinkButton("Link? ain't no way", "https://www.youtube.com/watch?v=dQw4w9WgXcQ")
        ])
        .WithActionRow([
            new SelectMenuBuilder("select-menu-w-1", [
                new SelectMenuOptionBuilder("Wires", "wires"),
                new SelectMenuOptionBuilder("More wires", "more wires")
                ])
            ])
        .WithActionRow([
            new ButtonBuilder("Button", "random-button-id-1", style: ButtonStyle.Danger),
            new ButtonBuilder("No way... A second one", "random-button-id-2", style: ButtonStyle.Success),
        ])
        .WithFile("attachment://wires.png"))
    .Build();

await RespondWithFileAsync(new FileAttachment("wires.png", "wires.png"), components: components);