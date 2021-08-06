using System;
using Zhongli.Data.Models.Logging;

namespace Zhongli.Services.Utilities
{
    public static class EnumExtensions
    {
        private static readonly GenericBitwise<ReprimandNoticeType> ReprimandNoticeTypeBitwise = new();
        private static readonly GenericBitwise<LoggingOptions> LoggingOptionsBitwise = new();

        public static LoggingOptions SetValue(this LoggingOptions options, LoggingOptions flag, bool? state)
            => LoggingOptionsBitwise.SetValue(options, flag, state);

        public static ReprimandNoticeType SetValue(this ReprimandNoticeType options, ReprimandNoticeType flag,
            bool? state)
            => ReprimandNoticeTypeBitwise.SetValue(options, flag, state);

        public static T SetValue<T>(this GenericBitwise<T> generic, T @enum, T flag, bool? state)
            where T : Enum
        {
            if (state is null)
                return generic.Xor(@enum, flag);

            return state.Value
                ? generic.Or(@enum, flag)
                : generic.And(@enum, generic.Not(flag));
        }
    }
}