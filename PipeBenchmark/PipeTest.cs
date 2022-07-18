using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace PipeBenchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class PipeTest
    {
        const string INPUT_FILE = @"data01.dat";
        const int DETAIL_COUNT = 10;

        #region char count
        const int HEADER_LEN = 39;
        const int DETAIL_LEN = 10;
        const int FOOTER_LEN = 39;

        readonly int _footerOffset = HEADER_LEN + DETAIL_LEN * DETAIL_COUNT;
        readonly int _totalLength = HEADER_LEN + DETAIL_LEN * DETAIL_COUNT + FOOTER_LEN;
        #endregion

        #region byte count
        const int HEADER_BYTE_LEN = 9 + 30 * 2;
        const int DETAIL_BYTE_LEN = 10;
        const int FOOTER_BYTE_LEN = 39 * 2;
        #endregion

        readonly byte[] _crlf = Encoding.ASCII.GetBytes("\r\n");
        readonly byte[] _comma = Encoding.ASCII.GetBytes(",");

        readonly byte[] _input;
        readonly Encoding _enc;

        public PipeTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _input = Enumerable.Repeat(File.ReadAllBytes(INPUT_FILE), 1000).SelectMany(x => x).ToArray();
            _enc = Encoding.GetEncoding("shift-jis");
        }

        #region Baseline
        [Benchmark(Baseline = true)]
        public async Task UseStreamReaderAsync()
        {
            var input = new MemoryStream(_input);
            var output = new MemoryStream();
            var details = Import(input);
            await WriteFileAsync(output, details);
#if DEBUG
            Console.Write(_enc.GetString(output.ToArray()));
#endif
        }

        private IEnumerable<Row> Import(Stream st)
        {
            var lineNum = 0;
            var details = new List<Row>();
            using (var sr = new StreamReader(st, _enc))
            {
                while (!sr.EndOfStream)
                {
                    lineNum++;
                    var line = sr.ReadLine();
                    if (line == null || line.Length != _totalLength)
                        throw new ApplicationException($"Data length differs line:{lineNum}");

                    if (string.IsNullOrEmpty(line))
                        break;

                    var header = line[..HEADER_LEN];
                    var footer = line[_footerOffset.._totalLength];

                    var offset = HEADER_LEN;
                    var offsetEnd = offset + DETAIL_LEN;
                    for (int i = 0; i < DETAIL_COUNT; i++)
                    {
                        details.Add(new Row()
                        {
                            Header01 = header[9..13],
                            Header02 = header[13..20],
                            Header03 = header[20..25],
                            Header04 = header[25..27],
                            Header05 = header[27..30],
                            Header06 = header[30..33],
                            Header07 = header[33..39],
                            Data = line[offset..offsetEnd],
                            Footer01 = footer[0..5],
                            Footer02 = footer[5..14],
                            Footer03 = footer[14..16],
                            Footer04 = footer[16..19],
                            Footer05 = footer[19..27],
                            Footer06 = footer[27..32],
                            Footer07 = footer[32..35],
                            Footer08 = footer[35..39],
                        });
                        offset += DETAIL_LEN;
                        offsetEnd += DETAIL_LEN;
                    }
                }
            }
            return details;
        }

        private async Task WriteFileAsync(Stream output, IEnumerable<Row> details)
        {
            using (var sw = new StreamWriter(output, _enc))
            {
                foreach (var d in details)
                {
                    var line = $"{d.Data},{d.Header01}{d.Header02}{d.Header03}{d.Header04}{d.Header05}{d.Header06}{d.Header07},{d.Footer01}{d.Footer02}{d.Footer03}{d.Footer04}{d.Footer05}{d.Footer06}{d.Footer07}{d.Footer08}";
                    await sw.WriteLineAsync(line);
                }
            }
        }

        public class Row
        {
            public string Header01 { get; set; } = "";
            public string Header02 { get; set; } = "";
            public string Header03 { get; set; } = "";
            public string Header04 { get; set; } = "";
            public string Header05 { get; set; } = "";
            public string Header06 { get; set; } = "";
            public string Header07 { get; set; } = "";
            public string Data { get; set; } = "";
            public string Footer01 { get; set; } = "";
            public string Footer02 { get; set; } = "";
            public string Footer03 { get; set; } = "";
            public string Footer04 { get; set; } = "";
            public string Footer05 { get; set; } = "";
            public string Footer06 { get; set; } = "";
            public string Footer07 { get; set; } = "";
            public string Footer08 { get; set; } = "";
        }
        #endregion

        #region Pipe_SequencePosition
        [Benchmark]
        public async Task Pipe_SeqPos_MemoryAsync()
        {
            var input = new MemoryStream(_input);
            var output = new MemoryStream();

            var pipe = new Pipe();
            var writing = FillPipeAsync(input, pipe.Writer);
            var reading = ReadSequencePositionAsync(output, pipe.Reader);
            await Task.WhenAll(writing, reading);
#if DEBUG
            Console.Write(_enc.GetString(output.ToArray()));
#endif
        }

        public static async Task FillPipeAsync(Stream stream, PipeWriter writer)
        {
            while (true)
            {
                try
                {
                    var memory = writer.GetMemory();
                    int byteRead = await stream.ReadAsync(memory);
                    if (byteRead == 0)
                        break;
                    writer.Advance(byteRead);
                }
                catch
                {
                    break;
                }
                var result = await writer.FlushAsync();
                if (result.IsCompleted)
                    break;
            }
            writer.Complete();
        }

        private async Task ReadSequencePositionAsync(Stream output, PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;
                SequencePosition? position;
                do
                {
                    position = buffer.PositionOf(_crlf.Last());
                    if (position != null)
                    {
                        try
                        {
                            OutputReadOnlySequence(output, buffer.Slice(0, position.Value));
                        }
                        catch (Exception ex)
                        {
                            reader.Complete(ex);
                            return;
                        }
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                } while (position != null);
                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted)
                    break;
            }
            reader.Complete();
        }

        private void OutputReadOnlySequence(Stream output, in ReadOnlySequence<byte> lineSegment)
        {
            var line = lineSegment.IsSingleSegment
                ? lineSegment.First
                : lineSegment.ToArray();

            var r = new LineMemory(line);
            r.Export(output, _comma, _crlf);
        }

        [Benchmark]
        public async Task Pipe_SeqPos_StructAsync()
        {
            var input = new MemoryStream(_input);
            var output = new MemoryStream();

            var pipe = new Pipe();
            var writing = FillPipeAsync(input, pipe.Writer);
            var reading = ReadSequencePositionStructAsync(output, pipe.Reader);
            await Task.WhenAll(writing, reading);
#if DEBUG
            Console.Write(_enc.GetString(output.ToArray()));
#endif
        }

        private async Task ReadSequencePositionStructAsync(Stream output, PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;
                SequencePosition? position;
                do
                {
                    position = buffer.PositionOf(_crlf.Last());
                    if (position != null)
                    {
                        try
                        {
                            OutputReadOnlySequenceStruct(output, buffer.Slice(0, position.Value));
                        }
                        catch (Exception ex)
                        {
                            reader.Complete(ex);
                            return;
                        }
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                } while (position != null);
                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted)
                    break;
            }
            reader.Complete();
        }

        private void OutputReadOnlySequenceStruct(Stream output, in ReadOnlySequence<byte> lineSegment)
        {
            var line = lineSegment.IsSingleSegment
                ? lineSegment.First.Span
                : lineSegment.ToArray().AsSpan();

            var r = new LineStruct(line);
            r.Export(output, _comma, _crlf);
        }
        #endregion

        #region Pipe_SequenceReader
        [Benchmark]
        public async Task Pipe_SeqReader_StructAsync()
        {
            var input = new MemoryStream(_input);
            var output = new MemoryStream();

            var pipe = new Pipe();
            var writing = FillPipeAsync(input, pipe.Writer);
            var reading = ReadPipeSequenceReaderAsync(output, pipe.Reader);
            await Task.WhenAll(writing, reading);
#if DEBUG
            Console.Write(_enc.GetString(output.ToArray()));
#endif
        }

        private async Task ReadPipeSequenceReaderAsync(Stream output, PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;
                ProcessSequenceReader(output, ref buffer);
                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted)
                    break;
            }
            reader.Complete();
        }

        private void ProcessSequenceReader(Stream output, ref ReadOnlySequence<byte> buffer)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            while (!sequenceReader.End)
            {
                while (sequenceReader.TryReadTo(out ReadOnlySpan<byte> line, _crlf))
                    OutputReadOnlySpan(output, line);

                buffer = buffer.Slice(sequenceReader.Position);
                sequenceReader.Advance(buffer.Length);
            }
        }

        private void OutputReadOnlySpan(Stream output, in ReadOnlySpan<byte> line)
        {
            var r = new LineStruct(line);
            r.Export(output, _comma, _crlf);
        }
        #endregion

        #region ReadStreamStruct
        [Benchmark]
        public void ReadStreamStruct()
        {
            var input = new MemoryStream(_input);
            var output = new MemoryStream();
            Convert(input, output);
#if DEBUG
            Console.Write(_enc.GetString(output.ToArray()));
#endif
        }

        private void Convert(Stream input, Stream output)
        {
            Span<byte> buffer = stackalloc byte[LineStruct.LENGTH];
            for (; ; )
            {
                var length = input.Read(buffer);
                if (length == 0)
                    break;

                if (!buffer.EndsWith(_crlf))
                    throw new ApplicationException("\r\n not found");

                var lineStuct = new LineStruct(buffer);
                lineStuct.Export(output, _comma, _crlf);
            }
        }
        #endregion


        #region Models 
        public class LineMemory
        {
            public const int LENGTH = 249; // include CRLF
            readonly ReadOnlyMemory<byte> _line;

            public LineMemory(ReadOnlyMemory<byte> line)
            {
                _line = line;
            }

            public ReadOnlySpan<byte> Header01 => _line[9..17].Span;
            public ReadOnlySpan<byte> Header02 => _line[17..31].Span;
            public ReadOnlySpan<byte> Header03 => _line[31..41].Span;
            public ReadOnlySpan<byte> Header04 => _line[41..45].Span;
            public ReadOnlySpan<byte> Header05 => _line[45..51].Span;
            public ReadOnlySpan<byte> Header06 => _line[51..57].Span;
            public ReadOnlySpan<byte> Header07 => _line[57..69].Span;

            public ReadOnlySpan<byte> Footer01 => _line[169..179].Span;
            public ReadOnlySpan<byte> Footer02 => _line[179..197].Span;
            public ReadOnlySpan<byte> Footer03 => _line[197..201].Span;
            public ReadOnlySpan<byte> Footer04 => _line[201..207].Span;
            public ReadOnlySpan<byte> Footer05 => _line[207..223].Span;
            public ReadOnlySpan<byte> Footer06 => _line[223..233].Span;
            public ReadOnlySpan<byte> Footer07 => _line[233..239].Span;
            public ReadOnlySpan<byte> Footer08 => _line[239..247].Span;

            public ReadOnlySpan<byte> GetDetail(int pos)
                => _line.Slice(HEADER_BYTE_LEN + (DETAIL_BYTE_LEN * pos), DETAIL_BYTE_LEN).Span;

            public void Export(Stream output, in ReadOnlySpan<byte> comma, in ReadOnlySpan<byte> crlf)
            {
                for (int i = 0; i < DETAIL_COUNT; i++)
                {
                    output.Write(GetDetail(i));
                    output.Write(comma);
                    output.Write(Header01);
                    output.Write(Header02);
                    output.Write(Header03);
                    output.Write(Header04);
                    output.Write(Header05);
                    output.Write(Header06);
                    output.Write(Header07);
                    output.Write(comma);
                    output.Write(Footer01);
                    output.Write(Footer02);
                    output.Write(Footer03);
                    output.Write(Footer04);
                    output.Write(Footer05);
                    output.Write(Footer06);
                    output.Write(Footer07);
                    output.Write(Footer08);
                    output.Write(crlf);
                }
            }
        }

        readonly ref struct LineStruct
        {
            public const int LENGTH = 249; // include CRLF
            readonly ReadOnlySpan<byte> _line;

            public LineStruct(ReadOnlySpan<byte> line)
            {
                _line = line;
            }

            public readonly ReadOnlySpan<byte> Header01 => _line[9..17];
            public readonly ReadOnlySpan<byte> Header02 => _line[17..31];
            public readonly ReadOnlySpan<byte> Header03 => _line[31..41];
            public readonly ReadOnlySpan<byte> Header04 => _line[41..45];
            public readonly ReadOnlySpan<byte> Header05 => _line[45..51];
            public readonly ReadOnlySpan<byte> Header06 => _line[51..57];
            public readonly ReadOnlySpan<byte> Header07 => _line[57..69];
            public readonly ReadOnlySpan<byte> Footer01 => _line[169..179];
            public readonly ReadOnlySpan<byte> Footer02 => _line[179..197];
            public readonly ReadOnlySpan<byte> Footer03 => _line[197..201];
            public readonly ReadOnlySpan<byte> Footer04 => _line[201..207];
            public readonly ReadOnlySpan<byte> Footer05 => _line[207..223];
            public readonly ReadOnlySpan<byte> Footer06 => _line[223..233];
            public readonly ReadOnlySpan<byte> Footer07 => _line[233..239];
            public readonly ReadOnlySpan<byte> Footer08 => _line[239..247];

            public readonly ReadOnlySpan<byte> GetDetail(int pos)
                => _line.Slice(HEADER_BYTE_LEN + (DETAIL_BYTE_LEN * pos), DETAIL_BYTE_LEN);

            public void Export(Stream output, in ReadOnlySpan<byte> comma, in ReadOnlySpan<byte> crlf)
            {
                for (int i = 0; i < DETAIL_COUNT; i++)
                {
                    output.Write(GetDetail(i));
                    output.Write(comma);
                    output.Write(Header01);
                    output.Write(Header02);
                    output.Write(Header03);
                    output.Write(Header04);
                    output.Write(Header05);
                    output.Write(Header06);
                    output.Write(Header07);
                    output.Write(comma);
                    output.Write(Footer01);
                    output.Write(Footer02);
                    output.Write(Footer03);
                    output.Write(Footer04);
                    output.Write(Footer05);
                    output.Write(Footer06);
                    output.Write(Footer07);
                    output.Write(Footer08);
                    output.Write(crlf);
                }
            }
        }
        #endregion
    }
}
