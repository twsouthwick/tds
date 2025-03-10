﻿using Microsoft.Protocols.Tds.Protocol;
using System.Buffers;
using System.Text;
using Xunit;

namespace Microsoft.Protocols.Tds.Tests;

public class OffsetWriterTests
{
    [Fact]
    public void Empty()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var payload = new ArrayBufferWriter<byte>();
        var offsetWriter = OffsetWriter.Create(0, InitialOffset, writer, payload);

        // Act
        offsetWriter.Complete();

        // Assert
        Assert.Equal(0, writer.WrittenCount);
        Assert.Equal(0, payload.WrittenCount);
    }

    [Fact]
    public void SingleEntry()
    {
        const string Message = "Hello";

        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var payload = new ArrayBufferWriter<byte>();
        var offsetWriter = OffsetWriter.Create(1, InitialOffset, writer, payload);

        // Act
        offsetWriter.WritePayload(Message);
        offsetWriter.Complete();

        // Assert
        var expected = new byte[] { 4 + InitialOffset, 0, (byte)Message.Length, 0 }
            .Concat(Encoding.Unicode.GetBytes(Message))
            .ToArray();
        Assert.Equal(expected, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void SingleEntryInitialOffset()
    {
        const string Message = "Hello";

        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        writer.Write(new byte[] { 1 });

        var payload = new ArrayBufferWriter<byte>();
        var offsetWriter = OffsetWriter.Create(1, writer.WrittenCount + InitialOffset, writer, payload);

        // Act
        offsetWriter.WritePayload(Message);
        offsetWriter.Complete();

        // Assert
        var expected = new byte[] { 1, 5 + InitialOffset, 0, (byte)Message.Length, 0 }
            .Concat(Encoding.Unicode.GetBytes(Message))
            .ToArray();
        Assert.Equal(expected, writer.WrittenSpan.ToArray());
    }

    private const int InitialOffset = 4;

    [Fact]
    public void TwoEntries()
    {
        const string Message1 = "Hello";
        byte[] Message2 = [1, 2, 3, 4];

        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var payload = new ArrayBufferWriter<byte>();

        var offsetWriter = OffsetWriter.Create(2, InitialOffset, writer, payload);

        // Act
        offsetWriter.WritePayload(Message1);
        offsetWriter.WritePayload(Message2);
        offsetWriter.Complete();

        // Assert
        var expected = new byte[] { 8 + InitialOffset, 0, (byte)Message1.Length, 0, 18 + InitialOffset, 0, (byte)Message2.Length, 0 }
            .Concat(Encoding.Unicode.GetBytes(Message1))
            .Concat(Message2)
            .ToArray();
        Assert.Equal(expected, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void TwoEntriesWithAdditionalDataInBetween()
    {
        const string Message1 = "Hello";
        byte[] Message2 = [1, 2, 3, 4];
        byte[] OtherData = [5, 6, 7];

        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var payload = new ArrayBufferWriter<byte>();
        var offsetWriter = OffsetWriter.Create(2, InitialOffset, writer, payload, additionalCount: OtherData.Length);

        // Act
        offsetWriter.WritePayload(Message1);
        offsetWriter.WriteOffset(OtherData);
        offsetWriter.WritePayload(Message2);
        offsetWriter.Complete();

        // Assert
        var expected = new byte[] { 11 + InitialOffset, 0, (byte)Message1.Length, 0, 5, 6, 7, 21 + InitialOffset, 0, 4, 0 }
            .Concat(Encoding.Unicode.GetBytes(Message1))
            .Concat(Message2)
            .ToArray();
        Assert.Equal(expected, writer.WrittenSpan.ToArray());
    }
}
