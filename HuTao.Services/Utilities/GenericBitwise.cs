using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HuTao.Services.Utilities;

public class GenericBitwise<T> where T : Enum
{
    private readonly Func<T, T, T> _and = And().Compile();
    private readonly Func<T, T, T> _or = Or().Compile();
    private readonly Func<T, T, T> _xor = Xor().Compile();
    private readonly Func<T, T> _not = Not().Compile();

    public T All()
    {
        var allFlags = Enum.GetValues(typeof(T)).Cast<T>();
        return Or(allFlags);
    }

    public T And(T value1, T value2) => _and(value1, value2);

    public T And(IEnumerable<T> list) => list.Aggregate(And);

    public T Not(T value) => _not(value);

    public T Or(T value1, T value2) => _or(value1, value2);

    public T Or(IEnumerable<T> list) => list.Aggregate(Or);

    public T Xor(T value1, T value2) => _xor(value1, value2);

    public T Xor(IEnumerable<T> list) => list.Aggregate(Xor);

    private static Expression<Func<T, T, T>> And()
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(T));
        var v1 = Expression.Parameter(typeof(T));
        var v2 = Expression.Parameter(typeof(T));

        return Expression.Lambda<Func<T, T, T>>(
            Expression.Convert(
                Expression.And( // combine the flags with an AND
                    Expression.Convert(v1,
                        underlyingType), // convert the values to a bit maskable type (i.e. the underlying numeric type of the enum)
                    Expression.Convert(v2, underlyingType)
                ),
                typeof(T) // convert the result of the AND back into the enum type
            ),
            v1, // the first argument of the function
            v2  // the second argument of the function
        );
    }

    private static Expression<Func<T, T, T>> Or()
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(T));
        var v1 = Expression.Parameter(typeof(T));
        var v2 = Expression.Parameter(typeof(T));

        return Expression.Lambda<Func<T, T, T>>(
            Expression.Convert(
                Expression.Or( // combine the flags with an OR
                    Expression.Convert(v1,
                        underlyingType), // convert the values to a bit maskable type (i.e. the underlying numeric type of the enum)
                    Expression.Convert(v2, underlyingType)
                ),
                typeof(T) // convert the result of the OR back into the enum type
            ),
            v1, // the first argument of the function
            v2  // the second argument of the function
        );
    }

    private static Expression<Func<T, T, T>> Xor()
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(T));
        var v1 = Expression.Parameter(typeof(T));
        var v2 = Expression.Parameter(typeof(T));

        return Expression.Lambda<Func<T, T, T>>(
            Expression.Convert(
                Expression.ExclusiveOr( // combine the flags with an XOR
                    Expression.Convert(v1,
                        underlyingType), // convert the values to a bit maskable type (i.e. the underlying numeric type of the enum)
                    Expression.Convert(v2, underlyingType)
                ),
                typeof(T) // convert the result of the OR back into the enum type
            ),
            v1, // the first argument of the function
            v2  // the second argument of the function
        );
    }

    private static Expression<Func<T, T>> Not()
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(T));
        var v1 = Expression.Parameter(typeof(T));

        return Expression.Lambda<Func<T, T>>(
            Expression.Convert(
                Expression.Not( // ~
                    Expression.Convert(v1, underlyingType)
                ),
                typeof(T) // convert the result of the tilde back into the enum type
            ),
            v1 // the argument of the function
        );
    }
}