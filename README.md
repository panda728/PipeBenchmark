# PipeBenchmark

|                     Method |      Mean |     Error |    StdDev | Ratio |      Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|--------------------------- |----------:|----------:|----------:|------:|-----------:|-----------:|----------:|----------:|
|       UseStreamReaderAsync | 644.81 ms | 12.566 ms | 14.959 ms |  1.00 | 49000.0000 | 16000.0000 | 4000.0000 |    373 MB |
|    Pipe_SeqPos_MemoryAsync |  93.94 ms |  1.866 ms |  3.726 ms |  0.14 |  1666.6667 |  1333.3333 | 1333.3333 |    129 MB |
|    Pipe_SeqPos_StructAsync |  89.44 ms |  1.715 ms |  2.106 ms |  0.14 |  1333.3333 |  1333.3333 | 1333.3333 |    128 MB |
| Pipe_SeqReader_StructAsync |  86.08 ms |  1.287 ms |  1.204 ms |  0.13 |  1333.3333 |  1333.3333 | 1333.3333 |    128 MB |
|           ReadStreamStruct |  80.17 ms |  1.572 ms |  3.066 ms |  0.13 |  1571.4286 |  1571.4286 | 1571.4286 |    128 MB |
