using System;
using System.Globalization;
using System.IO;
using Voxelmetric.Code.Utilities;
using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Utilities.Noise;

namespace Voxelmetric.Examples
{
    class VoxelmetricBenchmarks: MonoBehaviour
    {
        void Awake()
        {
            Benchmark_Modulus3();
            Benchmark_AbsValue();
            Benchmark_3D_to_1D_Index();
            Benchmark_1D_to_3D_Index();
            Benchmark_Noise();
            Benchmark_Noise_Dowsampling();
            Application.Quit();
        }

        void Benchmark_Modulus3()
        {
            Debug.Log("Bechmark - mod3");
            using (StreamWriter writer = File.CreateText("perf_mod3.txt"))
            {
                uint[] number = { 0 };
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = number[0] % 3;
                    }, 1000000
                    );
                Debug.LogFormat("Mod3 -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Mod3 -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                
                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Helpers.Mod3(number[0]);
                    }, 1000000
                    );
                Debug.LogFormat("Mod3 mersenne -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Mod3 mersenne -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_AbsValue()
        {
            Debug.Log("Bechmark - abs");
            using (StreamWriter writer = File.CreateText("perf_abs.txt"))
            {
                int[] number = {0};
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Mathf.Abs(number[0]);
                    }, 1000000
                    );
                Debug.LogFormat("Mathf.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Mathf.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Math.Abs(number[0]);
                    }, 1000000
                    );
                Debug.LogFormat("Math.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Math.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = number[0]<0 ? -number[0] : number[0];
                    }, 1000000
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
                    }, 1000000
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
                    }, 1000000
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
                    }, 1000000
                    );
                Debug.LogFormat("Helpers.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Helpers.Abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_3D_to_1D_Index()
        {
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
                    }, 1000000
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
                    }, 1000000
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
                    }, 1000000
                    );
                Debug.LogFormat("GetChunkIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetChunkIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                 number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_1D_to_3D_Index()
        {
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
                    }, 1000000
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
                    }, 1000000
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
                    }, 1000000
                    );
                Debug.LogFormat("GetChunkIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetChunkIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1],
                                 number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_Noise()
        {
            FastNoise noise = new FastNoise(0);

            Debug.Log("Bechmark - 1D, 2D, 3D noise");
            using (StreamWriter writer = File.CreateText("perf_noise.txt"))
            {
                float[] number = {0};
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        for (int y = 0; y < Env.ChunkSize; y++)
                            for (int z = 0; z < Env.ChunkSize; z++)
                                for (int x = 0; x < Env.ChunkSize; x++)
                                    number[0] += noise.SingleSimplex(0, x, z);
                    }, 10);
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
                    }, 10);
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
            FastNoise noise = new FastNoise(0);

            Debug.Log("Bechmark - 1D, 2D, 3D noise downsampling");
            using (StreamWriter writer = File.CreateText("perf_noise_downsampling.txt"))
            {
                for (int i = 1; i <= 3; i++)
                {
                    NoiseItem ni = new NoiseItem { noiseGen = new NoiseInterpolator() };
                    ni.noiseGen.SetInterpBitStep(Env.ChunkSize, i);
                    ni.lookupTable = Helpers.CreateArray1D<float>(ni.noiseGen.Size * ni.noiseGen.Size);

                    float[] number = {0};
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            PrepareLookupTable2D(noise, ni);
                            for (int y = 0; y < Env.ChunkSize; y++)
                                for (int z = 0; z < Env.ChunkSize; z++)
                                    for (int x = 0; x < Env.ChunkSize; x++)
                                        number[0] += ni.noiseGen.Interpolate(x, z, ni.lookupTable);
                        }, 10);
                    Debug.LogFormat("noise.Generate 2D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                    t.ToString(CultureInfo.InvariantCulture), i);
                    writer.WriteLine("noise.Generate 2D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                     t.ToString(CultureInfo.InvariantCulture), i);
                }

                for (int i = 1; i <= 3; i++)
                {
                    NoiseItem ni = new NoiseItem { noiseGen = new NoiseInterpolator() };
                    ni.noiseGen.SetInterpBitStep(Env.ChunkSize, i);
                    ni.lookupTable = Helpers.CreateArray1D<float>(ni.noiseGen.Size * ni.noiseGen.Size * ni.noiseGen.Size);

                    float[] number = {0};
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            PrepareLookupTable3D(noise, ni);
                            for (int y = 0; y<Env.ChunkSize; y++)
                                for (int z = 0; z<Env.ChunkSize; z++)
                                    for (int x = 0; x<Env.ChunkSize; x++)
                                        number[0] += ni.noiseGen.Interpolate(x, y, z, ni.lookupTable);
                        }, 10);
                    Debug.LogFormat("noise.Generate 3D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                    t.ToString(CultureInfo.InvariantCulture), i);
                    writer.WriteLine("noise.Generate 3D -> out:{0}, time:{1}, downsample factor {2}", number[0],
                                     t.ToString(CultureInfo.InvariantCulture), i);
                }
            }
        }
    }
}
