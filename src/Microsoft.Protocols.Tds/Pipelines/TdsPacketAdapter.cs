using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds;

internal sealed class TdsPacketAdapter : IDuplexPipe
{
    public TdsPacketAdapter(IDuplexPipe other)
    {
        Input = new Reader(this);
        Output = new Writer(this);
        InnerPipe = other;
    }

    public IDuplexPipe InnerPipe { get; set; }

    public TdsType Type { get; set; }

    PipeReader IDuplexPipe.Input => Input;

    PipeWriter IDuplexPipe.Output => Output;

    private Reader Input { get; }

    private Writer Output { get; }

    private sealed class Reader(TdsPacketAdapter adapter) : PipeReader
    {
        private ReadOnlySequence<byte> _buffer = ReadOnlySequence<byte>.Empty;

        private PipeReader InnerReader => adapter.InnerPipe.Input;

        public override void AdvanceTo(SequencePosition consumed)
        {
            _buffer = _buffer.Slice(consumed);
            InnerReader.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            _buffer = _buffer.Slice(consumed);
            InnerReader.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            _buffer = ReadOnlySequence<byte>.Empty;
            InnerReader.CancelPendingRead();
        }

        public override void Complete(Exception? exception = null)
        {
            InnerReader.Complete(exception);
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (!_buffer.IsEmpty)
            {
                return new ReadResult(_buffer, false, false);
            }

            var headerResult = await InnerReader.ReadAtLeastAsync(8, cancellationToken);

            if (headerResult.IsCompleted)
            {
                return headerResult;
            }

            var expectedLength = ReadHeader(headerResult.Buffer) - 8;

            var payload = await InnerReader.ReadAtLeastAsync(expectedLength, cancellationToken);

            var actualResult = new ReadResult(payload.Buffer.Slice(0, expectedLength), payload.IsCanceled, payload.IsCompleted);
            _buffer = actualResult.Buffer;

            return actualResult;
        }

        private int ReadHeader(in ReadOnlySequence<byte> headerResult)
        {
            Span<byte> buffer = stackalloc byte[8];
            var headerBytes = headerResult.Slice(0, 8);
            headerBytes.CopyTo(buffer);
            InnerReader.AdvanceTo(headerBytes.End);

            var header = MemoryMarshal.Read<TdsOptionsHeader>(buffer);

            adapter.Type = (TdsType)header.Type;

            return header.GetLength();
        }

        public override bool TryRead(out ReadResult result)
        {
            if (_buffer.Length > 0)
            {
                result = new(_buffer, false, false);
                return true;
            }

            result = default;
            return false;
        }
    }

    private sealed class Writer(TdsPacketAdapter adapter) : PipeWriter
    {
        private readonly ArrayBufferWriter<byte> _buffer = new();

        private PipeWriter InnerWriter => adapter.InnerPipe.Output;

        public override void Advance(int bytes) => _buffer.Advance(bytes);

        public override void CancelPendingFlush() => InnerWriter.CancelPendingFlush();

        public override void Complete(Exception? exception = null) => InnerWriter.Complete(exception);

        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_buffer.WrittenCount == 0)
            {
                return new FlushResult(false, false);
            }

            InnerWriter.WriteHeader(adapter.Type, (short)_buffer.WrittenCount);
            InnerWriter.Write(_buffer.WrittenSpan);

            var result = await InnerWriter.FlushAsync(cancellationToken);

            _buffer.ResetWrittenCount();

            return result;
        }

        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);
    }
}
