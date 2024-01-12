using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Protocols.Tds.Tests;

public class PreLoginTests
{
    [Fact]
    public async Task Test1Async()
    {
        byte[] expected =
        [
            // HEADER
            0x12,
            0x01,
            0x00,
            0x3A,
            0x00,
            0x00,
            0x01,
            0x00,

            // OPTION Version
            0x00,
            0x00,
            0x24,
            0x00,
            0x06,
            // OPTION Encrypt
            0x01,
            0x00,
            0x2A,
            0x00,
            0x01,
            // OPTION Instance
            0x02,
            0x00,
            0x2B,
            0x00,
            0x01,
            // OPTION ThreadId
            0x03,
            0x00,
            0x2C,
            0x00,
            0x04,
            // OPTION Mars
            0x04,
            0x00,
            0x30,
            0x00,
            0x01,
            // OPTION TraceId
            0x05,
            0x00,
            0x31,
            0x00,
            0x00,
            // OPTION FedAuthRequired
            0x06,
            0x00,
            0x31,
            0x00,
            0x01,

            0xFF,

            // DATA Version
            0x00,
            0x01,
            0x00,
            0x00,
            0x00,
            0x00,
            // DATA Encrypt
            0x00,
            // DATA Instance
            0x00,
            // DATA ThreadId
            0x00,
            0x00,
            0x00,
            0x00,
            // DATA Mars
            0x01,
            // DATA TraceId
            // DATA FedAuthRequired
            0x01,
        ];

        // Arrange
        var context = new TdsConnectionContext();
        var connectionFeature = new TestConnection(context);
        var pipeline = TdsConnectionBuilder.Create()
            .UseDefaultPacketProcessor()
            .Use((ctx, next) =>
            {
                ctx.Features.Set<ITdsConnectionFeature>(connectionFeature);
                return next(ctx);
            })
            .Use(async (ctx, next) =>
            {
                await ctx.SendPacketAsync(TdsType.PreLogin);
            })
            .Build();

        // Act
        await pipeline(context);

        // Assert
        Assert.Collection(connectionFeature.Written,
            c => Assert.Equal(c, expected));
    }

    private sealed class TestConnection(TdsConnectionContext context) : ITdsConnectionFeature
    {
        public ValueTask ReadPacketAsync(ITdsPacket packet)
        {
            throw new NotImplementedException();
        }

        public List<byte[]> Written { get; } = new();

        public TdsType Type => throw new NotImplementedException();

        public ValueTask WritePacket(ITdsPacket packet)
        {
            var writer = new ArrayBufferWriter<byte>();
            packet.Write(context, writer);
            Written.Add(writer.WrittenMemory.ToArray());

            return ValueTask.CompletedTask;
        }
    }
}