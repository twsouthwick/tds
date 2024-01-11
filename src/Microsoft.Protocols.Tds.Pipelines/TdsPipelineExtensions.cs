// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Protocols.Tds.Features;
using System.IO.Pipelines;

namespace Microsoft.Protocols.Tds;

public static class TdsPipelineExtensions
{
    public static IDuplexPipe AddTdsPipeline(this TdsConnectionContext ctx, IDuplexPipe pipe)
    {
        var tdsPacketTransport = new TdsPacketAdapter(pipe);
        var sslFeature = new SslDuplexPipeFeature(ctx, tdsPacketTransport);

        ctx.Features.Set<IPacketFeature>(tdsPacketTransport);
        ctx.Features.Set<ISslFeature>(sslFeature);

        return sslFeature;
    }
}
