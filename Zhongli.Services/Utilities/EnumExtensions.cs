using System;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation;

namespace Zhongli.Services.Utilities
{
    public static class EnumExtensions
    {
        private static readonly GenericBitwise<ReprimandNoticeType> ReprimandNoticeTypeBitwise = new();
        private static readonly GenericBitwise<ReprimandOptions> ReprimandOptionsBitwise = new();
        private static readonly GenericBitwise<LogType> LogTypeBitwise = new();

        public static LogType SetValue(this LogType options, LogType flag,
            bool? state)
            => LogTypeBitwise.SetValue(options, flag, state);

        public static ReprimandNoticeType SetValue(this ReprimandNoticeType options, ReprimandNoticeType flag,
            bool? state)
            => ReprimandNoticeTypeBitwise.SetValue(options, flag, state);

        public static ReprimandOptions SetValue(this ReprimandOptions options, ReprimandOptions flag, bool? state)
            => ReprimandOptionsBitwise.SetValue(options, flag, state);

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