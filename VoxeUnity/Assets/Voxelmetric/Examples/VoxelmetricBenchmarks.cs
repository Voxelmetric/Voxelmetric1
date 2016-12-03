using System;
using System.Globalization;
using System.IO;
using Assets.Voxelmetric.Code.Utilities;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Utilities;

namespace Assets.Voxelmetric.Examples
{
    class VoxelmetricBenchmarks : MonoBehaviour
    {
        void Awake()
        {
            Benchmark_AbsValue();
            Benchmark_3D_to_1D_Index();
            Benchmark_1D_to_3D_Index();
            Benchmark_Noise();
            Application.Quit();
        }

        void Benchmark_AbsValue()
        {
            Debug.Log("Bechmark - abs");
            using (StreamWriter writer = File.CreateText("perf_abs.txt"))
            {
                int[] number = { 0 };
                double t = Clock.BenchmarkTime(() =>
                                               {
                                                   ++number[0];
                                                   number[0] = Mathf.Abs(number[0]);
                                               }, 1000000
                    );
                Debug.LogFormat("Mathf.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Mathf.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(() =>
                                        {
                                            ++number[0];
                                            number[0] = Math.Abs(number[0]);
                                        }, 1000000
                    );
                Debug.LogFormat("Math.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Math.abs -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(() =>
                                        {
                                            ++number[0];
                                            number[0] = number[0]<0 ? -number[0] : number[0];
                                        }, 1000000
                    );
                Debug.LogFormat("i < 0 ? -i : i -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("i < 0 ? -i : i -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(() =>
                                        {
                                            ++number[0];
                                            int mask = number[0]>>31;
                                            number[0] = (number[0]+mask)^mask;
                                        }, 1000000
                    );
                Debug.LogFormat("(i + mask) ^ mask -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("(i + mask) ^ mask -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(() =>
                                        {
                                            ++number[0];
                                            number[0] = (number[0]+(number[0]>>31))^(number[0]>>31);
                                        }, 1000000
                    );
                Debug.LogFormat("(i + (i >> 31)) ^ (i >> 31) -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("(i + (i >> 31)) ^ (i >> 31) -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(() =>
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
                int[] number = { 0, 0, 0, 0 };
                double t = Clock.BenchmarkTime(() =>
                                               {
                                                   ++number[0];
                                                   Helpers.GetIndex3DFrom1D(number[0], out number[1], out number[2],
                                                                            out number[3], Env.ChunkSize, Env.ChunkSize);
                                               }, 1000000
                    );
                Debug.LogFormat("GetIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(() =>
                                        {
                                            ++number[0];
                                            Helpers.GetIndex3DFrom1D(number[0], out number[1], out number[2],
                                                                          out number[3], 33, 33);
                                        }, 1000000
                    );
                Debug.LogFormat("GetIndex3DFrom1D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex3DFrom1D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(() =>
                                        {
                                            ++number[0];
                                            Helpers.GetChunkIndex3DFrom1D(number[0], out number[1], out number[2],
                                                                          out number[3]);
                                        }, 1000000
                    );
                Debug.LogFormat("GetChunkIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetChunkIndex3DFrom1D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_1D_to_3D_Index()
        {
            Debug.Log("Bechmark - 1D to 3D index calculation");
            using (StreamWriter writer = File.CreateText("perf_1d_to_3d_index.txt"))
            {
                int[] number = { 0, 0, 0, 0 };
                double t = Clock.BenchmarkTime(() =>
                                               {
                                                   ++number[1];
                                                   ++number[2];
                                                   ++number[3];
                                                   number[0] = Helpers.GetIndex1DFrom3D(number[1], number[2], number[3],
                                                                                        Env.ChunkSize, Env.ChunkSize);
                                               }, 1000000
                    );
                Debug.LogFormat("GetIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(() =>
                {
                    ++number[1];
                    ++number[2];
                    ++number[3];
                    number[0] = Helpers.GetIndex1DFrom3D(number[1], number[2], number[3], 33, 33);
                }, 1000000
                    );
                Debug.LogFormat("GetIndex1DFrom3D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetIndex1DFrom3D non_pow_of_2 -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(() =>
                                        {
                                            ++number[1];
                                            ++number[2];
                                            ++number[3];
                                            number[0] = Helpers.GetChunkIndex1DFrom3D(number[1], number[2], number[3]);
                                        }, 1000000
                    );
                Debug.LogFormat("GetChunkIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("GetChunkIndex1DFrom3D -> out:{0}, x:{1},y:{2},z:{3}, time:{4}", number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture));
            }
        }

        void Benchmark_Noise()
        {
            Noise noise = new Noise("benchmark");

            Debug.Log("Bechmark - 1D, 2D, 3D noise");
            using (StreamWriter writer = File.CreateText("perf_noise.txt"))
            {
                float[] number = { 0, 0, 0, 0 };
                double t = Clock.BenchmarkTime(() => {
                            number[1] += 1.0f;
                            number[0] += noise.Generate(number[1]);
                        }, 1000000);
                Debug.LogFormat("noise.Generate 1D -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("noise.Generate 1D -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(() => {
                            number[1] += 1.0f;
                            number[2] += 1.0f;
                            number[0] += noise.Generate(number[1], number[2]);
                        }, 1000000);
                Debug.LogFormat("noise.Generate 2D -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("noise.Generate 2D -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(() => {
                            number[1] += 1.0f;
                            number[2] += 1.0f;
                            number[3] += 1.0f;
                            number[0] += noise.Generate(number[1], number[2], number[3]);
                        }, 1000000);
                Debug.LogFormat("noise.Generate 3D -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("noise.Generate 3D -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
