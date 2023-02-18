// Note: These tools are copied from my personal code reuse libraries - Ross Wright

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RossWright;

public static class Extenstions
{
    /// <summary>
    /// Returns a multi-line string for an exception that includes inner exceptions and stack traces
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static string ToBetterString(this Exception exception)
    {
        var message = new StringBuilder();
        Exception? inner = exception;
        do
        {
            message.AppendLine(inner.Message);
            if (inner.StackTrace != null)
                message.AppendLine(inner.StackTrace);
            inner = inner.InnerException;
            if (inner != null)
                message.AppendLine("Inner Exception:");
        }
        while (inner != null);

        return message.ToString();
    }

    /// <summary>
    /// Returns items with a distinct value for the given predicate
    /// </summary>
    /// <param name="source"></param>
    /// <param name="getValue"></param>
    /// <returns></returns>
    public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, Func<TSource?, object?> getValue) =>
        source.Distinct(new EasyDistinctEqualityComparer<TSource>(getValue));
    class EasyDistinctEqualityComparer<TSource> : IEqualityComparer<TSource>
    {
        public EasyDistinctEqualityComparer(Func<TSource?, object?> getValue) { _getValue = getValue; }
        readonly Func<TSource?, object?> _getValue;
        public bool Equals(TSource? x, TSource? y) =>
            _getValue(x)?.Equals(_getValue(y)) ?? _getValue(y) is null;
        public int GetHashCode([DisallowNull] TSource obj) => _getValue(obj)?.GetHashCode() ?? 0;
    }
}
