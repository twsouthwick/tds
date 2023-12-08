using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

public static class PacketOptionsBuilderExtensions
{
    public static IPacketBuilder AddOption(this IPacketBuilder builder, Action<IList<IPacketOption>> configure)
    {
        var list = new List<IPacketOption>();
        configure(list);
        builder.AddWriter(new OptionWriter(list, builder.Services.GetRequiredService<ObjectPool<ArrayBufferWriter<byte>>>()).Write);
        return builder;
    }

    private sealed class OptionWriter(IList<IPacketOption> options, ObjectPool<ArrayBufferWriter<byte>> pool)
    {
        private readonly IPacketOption[] _options = options.ToArray();

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            var offset = Marshal.SizeOf<TdsOptionItem>() * _options.Length + 1;
            var options = pool.Get();
            var payload = pool.Get();

            try
            {
                byte key = 0;

                foreach (var option in _options)
                {
                    var before = payload.WrittenCount;

                    option.Write(context, payload);

                    var after = payload.WrittenCount;

                    var optionItem = new TdsOptionItem
                    {
                        Type = key++,
                        Offset = (ushort)(offset + before),
                        Length = (ushort)(after - before),
                    };

                    options.Write(ref optionItem);
                }

                // Mark end of options
                options.Write((byte)255);

                writer.Write(options.WrittenSpan);
                writer.Write(payload.WrittenSpan);
            }
            finally
            {
                pool.Return(options);
                pool.Return(payload);
            }
        }
    }
}
