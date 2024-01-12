// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Protocols.Tds.Features;
using System.IO.Pipelines;

namespace Microsoft.Protocols.Tds;

public static class TdsPipelineExtensions
{
    public static IDuplexPipe AddTdsConnection(this TdsConnectionContext ctx, IDuplexPipe pipe)
    {
        var connectionFeature = new ConnectionPipelineFeature(ctx, pipe);

        ctx.Features.Set<ISslFeature>(connectionFeature);
        ctx.Features.Set<ITdsConnectionFeature>(connectionFeature);

        return connectionFeature;
    }
}
