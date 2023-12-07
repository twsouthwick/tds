using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Protocols.Tds.Tests;

public class PreLoginTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Test1Async()
    {
        byte[] expected =
        [
            // HEADER
            0x12, 0x01, 0x00, 0x3A, 0x00, 0x00, 0x01, 0x00, 
            
            // OPTION VersionOption
            0x00, 0x00, 0x24, 0x00, 0x06,
            // OPTION EncryptOption
            0x01, 0x00, 0x2A, 0x00, 0x01,
            // OPTION InstanceOption
            0x02, 0x00, 0x2B, 0x00, 0x01,
            // OPTION ThreadIdOption
            0x03, 0x00, 0x2C, 0x00, 0x04,
            // OPTION MarsOption
            0x04, 0x00, 0x30, 0x00, 0x01,
            // OPTION TraceIdOption
            0x05, 0x00, 0x31, 0x00, 0x00,
            // OPTION FedAuthRequiredOption
            0x06, 0x00, 0x31, 0x00, 0x01,

            0xFF,
            
            // DATA VersionOption
            0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 
            // DATA EncryptOption
            0x00, 
            // DATA InstanceOption
            0x00, 
            // DATA ThreadIdOption
            0x00, 0x00, 0x00, 0x00, 
            // DATA MarsOption
            0x01, 
            // DATA TraceIdOption
            // DATA FedAuthRequiredOption
            0x01,
        ];

        // Arrange
        var context = new TdsConnectionContext();
        var connectionFeature = new TestConnection(output, context);
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

    private sealed class TestConnection(ITestOutputHelper output, TdsConnectionContext context) : ITdsConnectionFeature
    {
        public ValueTask ReadPacketAsync(ITdsPacket packet)
        {
            throw new NotImplementedException();
        }

        public List<byte[]> Written { get; } = new();

        public ValueTask WritePacket(ITdsPacket packet)
        {
            var writer = new ArrayBufferWriter<byte>();
            packet.Write(context, writer);
            output.WriteLine(packet.ToString(writer.WrittenMemory, TdsPacketFormattingOptions.Code));
            Written.Add(writer.WrittenMemory.ToArray());

            return ValueTask.CompletedTask;
        }
    }
}