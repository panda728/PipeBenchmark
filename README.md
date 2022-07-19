# PipeBenchmark

|                     Method |       Mean |     Error |     StdDev | Ratio |      Gen 0 |      Gen 1 |     Gen 2 |     Allocated |
|--------------------------- |-----------:|----------:|-----------:|------:|-----------:|-----------:|----------:|--------------:|
|       UseStreamReaderAsync | 348.453 ms | 6.9207 ms | 12.3016 ms |  1.00 | 44000.0000 | 15000.0000 | 3000.0000 | 267,951,480 B |
|    Pipe_SeqPos_MemoryAsync |  12.857 ms | 0.0424 ms |  0.0354 ms |  0.04 |   203.1250 |          - |         - |   1,312,129 B |
|    Pipe_SeqPos_StructAsync |   8.830 ms | 0.0404 ms |  0.0378 ms |  0.03 |    62.5000 |          - |         - |     448,129 B |
| Pipe_SeqReader_StructAsync |   7.906 ms | 0.1535 ms |  0.2049 ms |  0.02 |    70.3125 |          - |         - |     446,217 B |
|           ReadStreamStruct |   5.128 ms | 0.0762 ms |  0.0595 ms |  0.01 |          - |          - |         - |         132 B |
