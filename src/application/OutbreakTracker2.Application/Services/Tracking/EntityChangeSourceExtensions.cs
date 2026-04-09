using System.Linq.Expressions;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public static class EntityChangeSourceExtensions
{
    /// <summary>
    /// Projects each entity update to the selected property and emits a
    /// <see cref="PropertyChange{T,TProp}"/> whenever the property value changes.
    /// The property name is inferred from the selector expression.
    /// </summary>
    public static Observable<PropertyChange<T, TProp>> TrackProperty<T, TProp>(
        this IEntityChangeSource<T> source,
        Expression<Func<T, TProp>> selector,
        IEqualityComparer<TProp>? comparer = null
    )
        where T : IHasId
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        Func<T, TProp> compiled = selector.Compile();
        string propertyName = ExtractPropertyName(selector);
        IEqualityComparer<TProp> eq = comparer ?? EqualityComparer<TProp>.Default;

        return source.Updated.SelectMany(change =>
        {
            TProp oldValue = compiled(change.Previous);
            TProp newValue = compiled(change.Current);

            return eq.Equals(oldValue, newValue)
                ? Observable.Empty<PropertyChange<T, TProp>>()
                : Observable.Return(new PropertyChange<T, TProp>(change.Current, oldValue, newValue, propertyName));
        });
    }

    private static string ExtractPropertyName<T, TProp>(Expression<Func<T, TProp>> selector)
    {
        return selector.Body is MemberExpression member ? member.Member.Name : selector.Body.ToString();
    }
}
