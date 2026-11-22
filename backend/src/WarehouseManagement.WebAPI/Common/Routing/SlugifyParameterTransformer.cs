// Chuyển token route PascalCase sang kebab-case cho URL API thân thiện.
using System.Text.RegularExpressions;

namespace WarehouseManagement.WebAPI.Routing;

// WarehouseStocks -> warehouse-stocks (dùng với RouteTokenTransformerConvention)
public partial class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    // Chèn dấu '-' giữa chữ thường và chữ hoa, rồi hạ chữ thường
    public string? TransformOutbound(object? value)
    {
        if (value is null) return null;
        return SlugRegex().Replace(value.ToString()!, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex SlugRegex();
}
