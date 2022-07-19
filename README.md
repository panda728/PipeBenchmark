# PipeBenchmark

|                     Method |       Mean |       Error |    StdDev | Ratio |      Gen 0 |      Gen 1 |     Gen 2 |     Allocated |
|--------------------------- |-----------:|------------:|----------:|------:|-----------:|-----------:|----------:|--------------:|
|       UseStreamReaderAsync | 347.374 ms | 179.7447 ms | 9.8524 ms |  1.00 | 44000.0000 | 15000.0000 | 3000.0000 | 267,951,784 B |
|    Pipe_SeqPos_MemoryAsync |  13.072 ms |   1.9735 ms | 0.1082 ms |  0.04 |   203.1250 |          - |         - |   1,312,132 B |
|    Pipe_SeqPos_StructAsync |   8.961 ms |   2.1101 ms | 0.1157 ms |  0.03 |    62.5000 |          - |         - |     448,170 B |
| Pipe_SeqReader_StructAsync |   7.552 ms |   0.6822 ms | 0.0374 ms |  0.02 |    70.3125 |          - |         - |     446,217 B |
|           ReadStreamStruct |   5.178 ms |   0.5896 ms | 0.0323 ms |  0.01 |          - |          - |         - |         132 B |
