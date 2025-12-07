using System.Globalization;
using System.Linq;
using System.Text;

namespace Jellyfin.Plugin.AccountSync.Extensions;

internal static class PropertyExtensions
{
    public static string PropertiesToString(this object obj)
    {
        return obj.GetType().GetProperties()
            .Select(info => (info.Name, Value: info.GetValue(obj, null) ?? "(null)"))
            .Aggregate(
                new StringBuilder("\n"),
                (sb, pair) => sb.AppendLine(CultureInfo.InvariantCulture, $"{pair.Name}: {pair.Value}"),
                sb => sb.ToString());
    }
}
