using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using Xunit;

namespace Microsoft.Protocols.Tds.Tests;

public class PreLoginTests
{
    [Fact(Skip = "Reworking the API")]
    public void Test1()
    {
        byte[] expected =
        [
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
            0x00,
            // DATA TraceId
            // DATA FedAuthRequired
            0x01,
        ];

#if FALSE
        // Arrange
        var context = new TdsConnectionContext();
        var writer = new ArrayBufferWriter<byte>();
        var pipeline = TdsConnectionBuilder.Create()
            .UseDefaultPacketProcessor()
            .Use((ctx, next) =>
            {
                ctx.GetPacket(TdsType.PreLogin).Write(ctx, writer);
                return ValueTask.CompletedTask;
            })
            .Build();

        // Act
        await pipeline(context);

        // Assert
        Assert.Equal(writer.WrittenSpan.ToArray(), expected);
#endif
    }
}