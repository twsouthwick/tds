using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Protocol;

public static class PreLoginPacketExtensions
{
    /// <summary>
    /// Adds implementations for the <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tds/60f56408-0188-4cd5-8b90-25c6f2423868">PRELOGIN</see> packet.
    /// </summary>
    public static void AddPreLogin(this IPacketCollectionBuilder builder)
        => builder.AddPacket(TdsType.PreLogin, packet =>
        {
            packet.AddOption(options =>
            {
                options.Add(new VersionOption());
                options.Add(new EncryptOption());
                options.Add(new InstanceOption());
                options.Add(new ThreadIdOption());
                options.Add(new MarsOption());
                options.Add(new TraceIdOption());
                options.Add(new FedAuthRequiredOption());
            });
        });

    private sealed class VersionOption : IPacketOption
    {
        private static readonly Version _emptyVersion = new(0, 1, 0, 0);

        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            var version = context.Features.Get<IEnvironmentFeature>()?.Version ?? _emptyVersion;

            writer.Write(version);
        }
    }

    private sealed class EncryptOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write((byte)0);
    }

    private sealed class InstanceOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.WriteNullTerminated(string.Empty);
    }

    private sealed class ThreadIdOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            if (context.Features.Get<IEnvironmentFeature>() is { } feature)
            {
                writer.Write(feature.ThreadId);
            }
            else
            {
                writer.Write(0);
            }
        }
    }

    private sealed class MarsOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
            var reader = new SequenceReader<byte>(data);
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(true);
    }

    private sealed class TraceIdOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            if (context.TraceId is { } traceId && context.Features.Get<ICorrelationFeature>() is { } feature)
            {
                writer.Write(traceId);
                writer.Write(feature.ActivityId);
                writer.Write(feature.SequenceId);
            }
        }
    }

    private sealed class FedAuthRequiredOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(true);
    }
}
