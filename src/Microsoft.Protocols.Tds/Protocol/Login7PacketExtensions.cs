using Microsoft.Protocols.Tds.Packets;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Protocol;

using DWORD = int;

public static class Login7PacketExtensions
{
    /// <summary>
    /// Adds implementations for the <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tds/773a62b6-ee89-4c02-9e5e-344882630aac">LOGIN7</see> packet
    /// </summary>
    public static void AddLogin7(this IPacketCollectionBuilder builder)
        => builder.AddPacket(TdsType.Login7)
            .AddLength()
            .AddOption(new TDSVersionOption())
            .AddOption(new PacketSizeOption())
            .AddOption(new ClientProgVerOption())
            .AddOption(new ConnectionIDOption());

    private sealed class TDSVersionOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            writer.Write((int)0x71);
        }
    }

    private sealed class PacketSizeOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            writer.Write(0x00_10_00_00);
        }
    }

    private sealed class ClientProgVerOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            writer.Write(0x00_00_00_07);
        }
    }

    private sealed class ConnectionIDOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            writer.Write(0x00_00_00_00);
        }
    }
}
