using System.Text;
using NorthwoodLib.Pools;

namespace Scp035.ApiFeatures;

internal static class TextFormatter
{
    internal static string Format(string text, params (string Key, object Value)[] replacements)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (replacements is null || replacements.Length is 0)
            return text;

        StringBuilder builder = StringBuilderPool.Shared.Rent(text);

        foreach ((string key, object value) in replacements)
            builder.Replace($"%{key}%", value?.ToString() ?? string.Empty);

        return StringBuilderPool.Shared.ToStringReturn(builder);
    }
}