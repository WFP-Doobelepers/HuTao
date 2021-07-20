using System;

namespace Zhongli.Services.Utilities
{
    public static class EnumExtensions
    {
        public static T SetValue<T>(this T @enum, T flag, bool? state) where T : Enum
        {
            var generic = new GenericBitwise<T>();
            return generic.SetValue(@enum, flag, state);
        }

        public static T SetValue<T>(this GenericBitwise<T> generic, T @enum, T flag, bool? state) where T : Enum
        {
            if (state is null)
                return generic.Xor(@enum, flag);

            return state.Value
                ? generic.Or(@enum, flag)
                : generic.And(@enum, generic.Not(flag));
        }
    }
}