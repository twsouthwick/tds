using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.Protocols.Tds;

internal static class FeatureCollectionExtensions
{
    public static T FetchRequired<T>(this ref FeatureReference<T> r, IFeatureCollection features)
        => r.Fetch(features) ?? throw new InvalidOperationException($"No {typeof(T).FullName} is available");
}
