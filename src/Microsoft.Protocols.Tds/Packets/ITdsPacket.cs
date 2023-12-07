﻿using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public interface ITdsPacket
{
    TdsType Type { get; }

    void Write(TdsConnectionContext context, IBufferWriter<byte> writer);

    void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data);

    ValueTask OnReadCompleteAsync(TdsConnectionContext context);

    string ToString(ReadOnlyMemory<byte> data, TdsPacketFormattingOptions options);
}

public enum TdsPacketFormattingOptions
{
    Default,
    Code
}
