using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Voxelmetric.Code.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities.Noise;
using Random = System.Random;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Examples
{
    class VoxelmetricBenchmarks: MonoBehaviour
    {
        void Awake()
        {
            Globals.InitWorkPool();
            Globals.InitIOPool();
            
            Benchmark_Modulus3();
            Benchmark_AbsValue();
            Benchmark_3D_to_1D_Index();
            Benchmark_1D_to_3D_Index();
            Benchmark_Noise();
            Benchmark_Noise_Dowsampling();
            Benchmark_Compression();
            Benchmark_MemCopy();
            Application.Quit();
        }

        void Benchmark_Modulus3()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - mod3");
            using (StreamWriter writer = File.CreateText("perf_mod3.txt"))
            {
                uint[] number = {0};
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = number[0]%3;
                    }, iters
                    );
                Debug.LogFormat("Mod3 -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Mod3 -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Helpers.Mod3(number[0]);
                    }, iters
                    );
                Debug.LogFormat("Mod3 mersenne -> out:{0}, time:{1}", number[0],
                                t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Mod3 mersenne -> out:{0}, time:{1}", number[0],
                                 t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_AbsValue()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - abs");
            using (StreamWriter writer = File.CreateText("perf_abs.txt"))
            {
                int[] number = {0};
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Mathf.Abs(number[0]);
                    }, iters
                    );
                Debug.LogFormat("Mathf.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Mathf.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Math.Abs(number[0]);
                    }, iters
                    );
                Debug.LogFormat("Math.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Math.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = number[0]<0 ? -number[0] : number[0];
                    }, iters
                    );
                Debug.LogFormat("i < 0 ? -i : i -> out:{0}, time:{1}", number[0],
                                t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("i < 0 ? -i : i -> out:{0}, time:{1}", number[0],
                                 t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        int mask = number[0]>>31;
                        number[0] = (number[0]+mask)^mask;
                    }, iters
                    );
                Debug.LogFormat("(i + mask) ^ mask -> out:{0}, time:{1}", number[0],
                                t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("(i + mask) ^ mask -> out:{0}, time:{1}", number[0],
                                 t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = (number[0]+(number[0]>>31))^(number[0]>>31);
                    }, iters
                    );
                Debug.LogFormat("(i + (i >> 31)) ^ (i >> 31) -> out:{0}, time:{1}", number[0],
                                t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("(i + (i >> 31)) ^ (i >> 31) -> out:{0}, time:{1}", number[0],
                                 t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Helpers.Abs(number[0]);
                    }, iters
                    );
                Debug.LogFormat("Helpers.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Helpers.Abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_3D_to_1D_Index()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - 3D to 1D index calculation");
            using (StreamWriter writer = File.CreateText("perf_3d_to_1d_index.txt"))
            {
                int[] number = {0, 0, 0, 0};
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        Helpers.GetIndex3DFrom1D(number[0], out number[1], out number[2],
                                                 out number[3], Env.ChunkSize, Env.ChunkSize);
                    }, iters
                    );
                Debug.LogFormat("GetIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                 number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        Helpers.GetIndex3DFrom1D(number[0], out number[1], out number[2],
                                                 out number[3], 33, 33);
                    }, iters
                    );
                Debug.LogFormat("GetIndex3DFrom1D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0],
                                number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex3DFrom1D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0],
                                 number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        Helpers.GetChunkIndex3DFrom1D(number[0], out number[1], out number[2],
                                                      out number[3]);
                    }, iters
                    );
                Debug.LogFormat("GetChunkIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetChunkIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                 number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_1D_to_3D_Index()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - 1D to 3D index calculation");
            using (StreamWriter writer = File.CreateText("perf_1d_to_3d_index.txt"))
            {
                int[] number = {0, 0, 0, 0};
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[1];
                        ++number[2];
                        ++number[3];
                        number[0] = Helpers.GetIndex1DFrom3D(number[1], number[2], number[3],
                                                             Env.ChunkSize, Env.ChunkSize);
                    }, iters
                    );
                Debug.LogFormat("GetIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                 number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[1];
                        ++number[2];
                        ++number[3];
                        number[0] = Helpers.GetIndex1DFrom3D(number[1], number[2], number[3], 33, 33);
                    }, iters
                    );
                Debug.LogFormat("GetIndex1DFrom3D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0],
                                number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex1DFrom3D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0],
                                 number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[1];
                        ++number[2];
                        ++number[3];
                        number[0] = Helpers.GetChunkIndex1DFrom3D(number[1], number[2], number[3]);
                    }, iters
                    );
                Debug.LogFormat("GetChunkIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetChunkIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                 number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_Noise()
        {
            const int iters = 10;
            FastNoise noise = new FastNoise(0);

            Debug.Log("Bechmark - 1D, 2D, 3D noise");
            using (StreamWriter writer = File.CreateText("perf_noise.txt"))
            {
                float[] number = {0};
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        for (int y = 0; y<Env.ChunkSize; y++)
                            for (int z = 0; z<Env.ChunkSize; z++)
                                for (int x = 0; x<Env.ChunkSize; x++)
                                    number[0] += noise.SingleSimplex(0, x, z);
                    }, iters);
                Debug.LogFormat("noise.Generate 2D -> out:{0}, time:{1}", number[0],
                                t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("noise.Generate 2D -> out:{0}, time:{1}", number[0],
                                 t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        for (int y = 0; y<Env.ChunkSize; y++)
                            for (int z = 0; z<Env.ChunkSize; z++)
                                for (int x = 0; x<Env.ChunkSize; x++)
                                    number[0] += noise.SingleSimplex(0, x, y, z);
                    }, iters);
                Debug.LogFormat("noise.Generate 3D -> out:{0}, time:{1}", number[0],
                                t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("noise.Generate 3D -> out:{0}, time:{1}", number[0],
                                 t.ToString(CultureInfo.InvariantCulture));
            }
        }

        private NoiseItem PrepareLookupTable2D(FastNoise noise, NoiseItem ni)
        {
            // Generate a lookup table
            int i = 0;
            for (int z = 0; z<ni.noiseGen.Size; z++)
            {
                float zf = (z<<ni.noiseGen.Step);

                for (int x = 0; x<ni.noiseGen.Size; x++)
                {
                    float xf = (x<<ni.noiseGen.Step);
                    ni.lookupTable[i++] = noise.SingleSimplex(0, xf, zf);
                }
            }

            return ni;
        }

        private NoiseItem PrepareLookupTable3D(FastNoise noise, NoiseItem ni)
        {
            // Generate a lookup table
            int i = 0;
            for (int y = 0; y<ni.noiseGen.Size; y++)
            {
                float yf = (y<<ni.noiseGen.Step);
                for (int z = 0; z<ni.noiseGen.Size; z++)
                {
                    float zf = (z<<ni.noiseGen.Step);
                    for (int x = 0; x<ni.noiseGen.Size; x++)
                    {
                        float xf = (x<<ni.noiseGen.Step);
                        ni.lookupTable[i++] = noise.SingleSimplex(0, xf, yf, zf);
                    }
                }
            }

            return ni;
        }

        void Benchmark_Noise_Dowsampling()
        {
            const int iters = 10;
            FastNoise noise = new FastNoise(0);

            Debug.Log("Bechmark - 1D, 2D, 3D noise downsampling");
            using (StreamWriter writer = File.CreateText("perf_noise_downsampling.txt"))
            {
                for (int i = 1; i<=3; i++)
                {
                    NoiseItem ni = new NoiseItem {noiseGen = new NoiseInterpolator()};
                    ni.noiseGen.SetInterpBitStep(Env.ChunkSize, i);
                    ni.lookupTable = Helpers.CreateArray1D<float>(ni.noiseGen.Size*ni.noiseGen.Size);

                    float[] number = {0};
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            PrepareLookupTable2D(noise, ni);
                            for (int y = 0; y<Env.ChunkSize; y++)
                                for (int z = 0; z<Env.ChunkSize; z++)
                                    for (int x = 0; x<Env.ChunkSize; x++)
                                        number[0] += ni.noiseGen.Interpolate(x, z, ni.lookupTable);
                        }, iters);
                    Debug.LogFormat("noise.Generate 2D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                    t.ToString(CultureInfo.InvariantCulture), i);
                    writer.WriteLine("noise.Generate 2D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                     t.ToString(CultureInfo.InvariantCulture), i);
                }

                for (int i = 1; i<=3; i++)
                {
                    NoiseItem ni = new NoiseItem {noiseGen = new NoiseInterpolator()};
                    ni.noiseGen.SetInterpBitStep(Env.ChunkSize, i);
                    ni.lookupTable = Helpers.CreateArray1D<float>(ni.noiseGen.Size*ni.noiseGen.Size*ni.noiseGen.Size);

                    float[] number = {0};
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            PrepareLookupTable3D(noise, ni);
                            for (int y = 0; y<Env.ChunkSize; y++)
                                for (int z = 0; z<Env.ChunkSize; z++)
                                    for (int x = 0; x<Env.ChunkSize; x++)
                                        number[0] += ni.noiseGen.Interpolate(x, y, z, ni.lookupTable);
                        }, iters);
                    Debug.LogFormat("noise.Generate 3D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                    t.ToString(CultureInfo.InvariantCulture), i);
                    writer.WriteLine("noise.Generate 3D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                     t.ToString(CultureInfo.InvariantCulture), i);
                }
            }
        }

        void Compression(StreamWriter writer, Chunk chunk, int blockTypes, int probabiltyOfChange)
        {
            const int iters = 100;
            var blocks = chunk.blocks;

            // Initialize the block array. Padded area contains zeros, the rest is random
            {
                Random r = new Random(0);
                ushort type = (ushort)r.Next(0, blockTypes);
                
                int index = 0;
                for (int y = 0; y < Env.ChunkSize; ++y)
                {
                    for (int z = 0; z < Env.ChunkSize; ++z)
                    {
                        for (int x = 0; x < Env.ChunkSize; ++x, ++index)
                        {
                            int prob = r.Next(0, 99);
                            if (prob<probabiltyOfChange)
                                type = (ushort)r.Next(0, blockTypes);
                            blocks.SetRaw(index, new BlockData(type));
                        }
                    }
                }
            }

            StringBuilder s = new StringBuilder();
            {
                s.AppendFormat("Bechmark - compression ({0} block types, probability of change: {1})", blockTypes, probabiltyOfChange);
                Debug.Log(s);
                writer.WriteLine(s);

                // Compression
                {
                    float[] number = { 0 };
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            blocks.Compress();
                        }, iters);

                    s.Remove(0, s.Length);
                    s.AppendFormat("Compression -> out:{0}, time:{1}, boxes created: {2}, mem: {3}/{4}", number[0],
                                   (t/iters).ToString(CultureInfo.InvariantCulture), blocks.BlocksCompressed.Count,
                                   blocks.BlocksCompressed.Count*StructSerialization.TSSize<BlockDataAABB>.ValueSize,
                                   Env.ChunkSizeWithPaddingPow3*StructSerialization.TSSize<BlockData>.ValueSize);
                }
                Debug.Log(s);
                writer.WriteLine(s);

                // Decompression
                {
                    float[] number = { 0 };
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            blocks.Decompress();
                        }, iters);

                    s.Remove(0, s.Length);
                    s.AppendFormat("Decompression -> out:{0}, time:{1}", number[0],
                                   (t/iters).ToString(CultureInfo.InvariantCulture));
                }
                Debug.Log(s);
                writer.WriteLine(s);
            }
        }

        void Benchmark_Compression()
        {
            Chunk chunk = new Chunk();
            chunk.Init(null, Vector3Int.zero);

            using (StreamWriter writer = File.CreateText("compression.txt"))
            {
                Compression(writer, chunk, 2, 100);
                Compression(writer, chunk, 4, 100);
                Compression(writer, chunk, 8, 100);
                Compression(writer, chunk, 12, 100);
                Compression(writer, chunk, 2, 20);
                Compression(writer, chunk, 4, 20);
                Compression(writer, chunk, 8, 20);
                Compression(writer, chunk, 12, 20);
                Compression(writer, chunk, 2, 10);
                Compression(writer, chunk, 4, 10);
                Compression(writer, chunk, 8, 10);
                Compression(writer, chunk, 12, 10);
            }
        }
        
        private class TestClass1
        {
            private readonly unsafe byte* m_blocks;
            private readonly IntPtr rawptr;

            public unsafe TestClass1()
            {
                // Force 16-bytes aligment
                rawptr = Marshal.AllocHGlobal(Env.ChunkSizePow3 * StructSerialization.TSSize<BlockData>.ValueSize + 16);
                var aligned = new IntPtr(16 * (((long)rawptr + 15) / 16));
                m_blocks = (byte*)aligned.ToPointer();
            }

            ~TestClass1()
            {
                Marshal.FreeHGlobal(rawptr);
            }
            
            public unsafe BlockData this[int i]
            {
                get
                {
                    return *((BlockData*)&m_blocks[i<<1]);
                }
                set
                {
                    *((BlockData*)&m_blocks[i<<1]) = value;
                }
            }
            
            public unsafe void Copy(byte[] src, int srcIndex, int dstIndex, int bytes)
            {
                fixed (byte *pSrc = &src[srcIndex])
                {
                    Utils.MemoryCopy(&m_blocks[dstIndex], pSrc, bytes);
                }
            }
        }

        private static BlockData[] bd2;
        
        private class TestClass2
        {
            private readonly BlockData[] m_blocks = Helpers.CreateArray1D<BlockData>(bd2.Length);
            public BlockData this[int i]
            {
                get { return m_blocks[i]; }
                set { m_blocks[i] = value; }
            }
            
            public void Copy(BlockData[] src, int srcIndex, int dstIndex, int length)
            {
                Array.Copy(src, srcIndex, m_blocks, dstIndex, length);
            }
        }
        
        void Benchmark_MemCopy()
        {
            int[] memItems =
            {
                32,
                64,
                128,
                256,
                Env.ChunkSizeWithPaddingPow2,
                Env.ChunkSizePow3
            };

            int[] iters =
            {
                1000000,
                1000000,
                50000,
                50000,
                10000,
                5000
            };
            
            Debug.Assert(memItems.Length==iters.Length);
            int maxItems = memItems[memItems.Length-1];

            byte[] bd1 = Helpers.CreateArray1D<byte>(maxItems * StructSerialization.TSSize<BlockData>.ValueSize);
            for (int i = 0; i<bd1.Length; i++)
                bd1[i] = 1;
            bd2 = Helpers.CreateArray1D<BlockData>(maxItems);
            for (int i = 0; i<bd2.Length; i++)
                bd2[i] = new BlockData(0x101);
            BlockData dummy = new BlockData(0x101);
            
            TestClass1 tc1 = new TestClass1();
            TestClass2 tc2 = new TestClass2();
            
            Debug.Log("Bechmark - memory copy");
            using (StreamWriter writer = File.CreateText("perf_memcpy.txt"))
            {
                for(int i=0; i<iters.Length; i++)
                {
                    int loops = iters[i];
                    int items = memItems[i];
                    int bytes = items * StructSerialization.TSSize<BlockData>.ValueSize;
                    
                    Debug.LogFormat("Bytes to copy: {0}", bytes);
                    writer.WriteLine("Bytes to copy: {0}", bytes);
                    
                    {
                        float[] number = {0};
                        double t = Clock.BenchmarkTime(
                            () =>
                            {
                                tc1.Copy(bd1, 0, 0, bytes);
                            }, loops);
                        
                        Debug.LogFormat("MemoryCopy -> out:{0}, time:{1}", number[0],
                                        t.ToString(CultureInfo.InvariantCulture));
                        writer.WriteLine("MemoryCopy -> out:{0}, time:{1}", number[0],
                                         t.ToString(CultureInfo.InvariantCulture));
                    }
                    for (int j = 0; j<items; j++)
                        Assert.IsTrue(tc1[j]==dummy);

                    {
                        float[] number = {0};
                        double t = Clock.BenchmarkTime(
                            () =>
                            {
                                tc2.Copy(bd2, 0, 0, items);
                            }, loops);
                        
                        Debug.LogFormat("ArrayCopy -> out:{0}, time:{1}", number[0],
                                        t.ToString(CultureInfo.InvariantCulture));
                        writer.WriteLine("ArrayCopy -> out:{0}, time:{1}", number[0],
                                         t.ToString(CultureInfo.InvariantCulture));
                    }
                    for (int j = 0; j<items; j++)
                        Assert.IsTrue(tc2[j]==dummy);
                }
            }
        }
    }
}
