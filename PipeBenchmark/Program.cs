using BenchmarkDotNet.Running;
using PipeBenchmark;
using System.IO.Pipes;

#if DEBUG

var test = new PipeTest();
await test.UseStreamReaderAsync();
await test.Pipe_SeqPos_MemoryAsync();
await test.Pipe_SeqPos_StructAsync();
await test.Pipe_SeqReader_StructAsync();
test.ReadStreamStruct();
Console.WriteLine("Press any key...");
Console.ReadLine();
#else
var summary = BenchmarkRunner.Run<PipeTest>();
#endif

