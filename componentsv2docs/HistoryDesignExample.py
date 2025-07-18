import hikari

components = [
    hikari.impl.SectionComponentBuilder(
        accessory=hikari.impl.LinkButtonBuilder(
            url="discord://-/guilds/923991820868911184/settings/members",
            label="View Member",
            emoji="👤",
        ),
        components=[
            hikari.impl.TextDisplayComponentBuilder(content="# <@1036096131970629722> History"),
        ]
    ),
    hikari.impl.ContainerComponentBuilder(
        components=[
            hikari.impl.SectionComponentBuilder(
                accessory=hikari.impl.ThumbnailComponentBuilder(
                    media="https://cdn.discordapp.com/avatars/852717789071278100/582ffa635f2632db6e24c5f5350ff780.png?size=4096",
                ),
                components=[
                    hikari.impl.TextDisplayComponentBuilder(content="hime.san (852717789071278100)"),
                    hikari.impl.TextDisplayComponentBuilder(content="Created <t:1744782660:R> <t:1744782660:f>\nJoined   <t:1744782660:R> <t:1744782660:f>\n\n-# - Warning 0/0 [0/0]\n-# - Notice 0/0 [0/0]\n-# - Ban 0/0 [0/0]\n-# - Kick 0/0 [0/0]\n-# - Note 0/0 [0/0]\n-# - Mute 0/0 [0/0]\n-# - Censored 0/0 [0/0]"),
                ]
            ),
        ]
    ),
    hikari.impl.SeparatorComponentBuilder(divider=True, spacing=hikari.SpacingType.LARGE,),
    hikari.impl.ContainerComponentBuilder(
        components=[
            hikari.impl.SectionComponentBuilder(
                accessory=hikari.impl.InteractiveButtonBuilder(
                    style=hikari.components.ButtonStyle.SECONDARY,
                    emoji="✏️",
                    custom_id="38e0e62e01d0479ab5e28139799344a9",
                ),
                components=[
                    hikari.impl.TextDisplayComponentBuilder(content="### Warning • [78b023b2]\n-# <@328166359483547650> <t:1744782660:d> <t:1744782660:t> • <t:1744782660:R>\n>>> Please avoid doomposting in theorycrafting discussions. Comments like \"why even play anymore\" are unproductive and discourage others. If you continue, further action may be taken."),
                ]
            ),
            hikari.impl.SeparatorComponentBuilder(divider=True, spacing=hikari.SpacingType.SMALL,),
            hikari.impl.TextDisplayComponentBuilder(content="-# - User warned for attempting to [bypass rules](https://discord.com/channels/763583452762734592/953149337813266452/1393572627343212744) and being argumetative about it. Again.\n-# - Then argued about it [even after that](https://discord.com/channels/763583452762734592/953149337813266452/1393607142635995227). Warn user immediately on similar infractions going forward. Keep an eye for exile but user likely should be banned from server rather than exile at this continued path"),
            hikari.impl.MessageActionRowBuilder(
                components=[
                    hikari.impl.TextSelectMenuBuilder(
                        custom_id="26d69d8fda87440eda77533b17f2f0ba",
                        placeholder="Action...",
                        options=[
                            hikari.impl.SelectOptionBuilder(
                                label="Forgive",
                                value="a295fa0156af4f39a193483677628c6b",
                                description="Forgive",
                                emoji="➖",
                            ),
                            hikari.impl.SelectOptionBuilder(
                                label="Delete",
                                value="7b4214c4cbd348b3923a1a019badd176",
                                emoji="🗑️",
                            ),
                        ]
                    ),
                ]
            ),
        ]
    ),
    hikari.impl.ContainerComponentBuilder(
        components=[
            hikari.impl.SectionComponentBuilder(
                accessory=hikari.impl.InteractiveButtonBuilder(
                    style=hikari.components.ButtonStyle.SECONDARY,
                    emoji="✏️",
                    custom_id="dd06ba2e3d114e0588953fd8f80f695e",
                ),
                components=[
                    hikari.impl.TextDisplayComponentBuilder(content="### Warning • [78b023b2]\n-# <@417236986181451778> <t:1744782660:d> <t:1744782660:t> • <t:1744782660:R>\nPosting NSFW or sexually suggestive content is not allowed in this server. This is your official warning. Any further infractions may result in a mute or ban. Please review the rules and keep all content safe for work."),
                ]
            ),
            hikari.impl.SeparatorComponentBuilder(divider=True, spacing=hikari.SpacingType.SMALL,),
            hikari.impl.TextDisplayComponentBuilder(content="-# - User posted suggestive image in #memes without a spoiler tag or content warning. Image was removed and user was informed.  Then argued about it [even after that](https://discord.com/channels/763583452762734592/953149337813266452/1393607142635995227).  \n-# - Warn user immediately on similar infractions going forward. "),
            hikari.impl.MediaGalleryComponentBuilder(
                items=[
                    hikari.impl.MediaGalleryItemBuilder(
                        media="https://media.discordapp.net/attachments/923991820868911187/1393701627306840114/776bd8f5c11e4867b9c7cab5aa667a1b.png?ex=68742149&is=6872cfc9&hm=ab737e1c47645fb07a0bbf893f777185611b39e4227e9cbd2d54f5fa1757253b&=&width=986&height=1395",
                        spoiler=True,
                    ),
                    hikari.impl.MediaGalleryItemBuilder(
                        media="https://media.discordapp.net/attachments/923991820868911187/1393701628023931051/ef380456c44e419bf05879c4b7a68f64.png?ex=68742149&is=6872cfc9&hm=e6324967af034c259c094eed7f5275c6c223a98275ad5f011e60f381e54344c2&=&width=986&height=1395",
                        spoiler=True,
                    ),
                ]
            ),
            hikari.impl.MessageActionRowBuilder(
                components=[
                    hikari.impl.TextSelectMenuBuilder(
                        custom_id="de05d2bc502144bce6f3ff2c7e13ab0f",
                        placeholder="Action...",
                        options=[
                            hikari.impl.SelectOptionBuilder(
                                label="Wild Alligator",
                                value="c9bd183dc7ca42dbeb0c7ed89a7d3e98",
                            ),
                        ]
                    ),
                ]
            ),
        ]
    ),
    hikari.impl.ContainerComponentBuilder(
        components=[
            hikari.impl.SectionComponentBuilder(
                accessory=hikari.impl.InteractiveButtonBuilder(
                    style=hikari.components.ButtonStyle.SECONDARY,
                    emoji="✏️",
                    custom_id="4c65ffab6fbf4d6bcb0510aa6d286ac5",
                ),
                components=[
                    hikari.impl.TextDisplayComponentBuilder(content="### Censor\n-# 78b023b2-bfe0-4633-a248-b9b1cdbd707a\n<@917873687254958181>  <t:1744782660:d> <t:1744782660:t> • <t:1744782660:R>\n[Suspicious Files]\nplugins.7z & themes.7z"),
                ]
            ),
            hikari.impl.MessageActionRowBuilder(
                components=[
                    hikari.impl.TextSelectMenuBuilder(
                        custom_id="fc191c7357d44056845aa3842efce7c3",
                        placeholder="Action...",
                        options=[
                            hikari.impl.SelectOptionBuilder(
                                label="Furry Hummingbird",
                                value="09636f0d58784270c029af44067b6795",
                            ),
                        ]
                    ),
                ]
            ),
        ]
    ),
    hikari.impl.MessageActionRowBuilder(
        components=[
            hikari.impl.TextSelectMenuBuilder(
                custom_id="6949f7ac774244039193fe4a888c201f",
                placeholder="Filter...",
                min_values=1,
                max_values=4,
                options=[
                    hikari.impl.SelectOptionBuilder(
                        label="All",
                        value="1ad9c287ea8e4a35ffa156440eb766e8",
                        emoji="⭐",
                        is_default=True,
                    ),
                    hikari.impl.SelectOptionBuilder(
                        label="Ban",
                        value="6efd802be4df4ef2fbd952103872577a",
                        emoji="🔨",
                        is_default=True,
                    ),
                    hikari.impl.SelectOptionBuilder(
                        label="Kick",
                        value="23c9c6404ece45fafd77facdb07d065d",
                        emoji="👞",
                        is_default=True,
                    ),
                    hikari.impl.SelectOptionBuilder(
                        label="Mute",
                        value="e9deea8203524c81ef12377da2e159b3",
                        emoji="🔇",
                        is_default=True,
                    ),
                ]
            ),
        ]
    ),
    hikari.impl.TextDisplayComponentBuilder(content="-# Requested by @hime.san"),
]