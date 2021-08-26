using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Discord.Reaction
{
    public class EmoteEntity : ReactionEntity
    {
        protected EmoteEntity() { }

        public EmoteEntity(Emote emote) : base(emote)
        {
            EmoteId    = emote.Id;
            IsAnimated = emote.Animated;
        }

        public bool IsAnimated { get; set; }

        public ulong EmoteId { get; set; }

        public override string ToString() => $"<{(IsAnimated ? "a" : string.Empty)}:{Name}:{EmoteId}>";
    }

    public class EmoteReactionConfiguration : IEntityTypeConfiguration<EmoteEntity>
    {
        public void Configure(EntityTypeBuilder<EmoteEntity> builder)
        {
            builder
                .HasIndex(e => e.EmoteId)
                .IsUnique();
        }
    }
}