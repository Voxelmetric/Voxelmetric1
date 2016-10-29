using System;
using System.Globalization;
using System.IO;
using Assets.Voxelmetric.Code.Utilities;
using UnityEngine;
using Voxelmetric.Code.Common;

namespace Assets.Voxelmetric.Examples
{
    class VoxelmetricBenchmarks : MonoBehaviour
    {
        void Awake()
        {
            Benchmark_AbsValue();
        }

        void Benchmark_AbsValue()
        {
            Debug.Log("Bechmark - abs");
            using (StreamWriter writer = File.CreateText("perf_abs.txt"))
            {
                int[] number = {0};
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
                    number[0] = number[0] < 0 ? -number[0] : number[0];
                }, 1000000
                    );
                Debug.LogFormat("i < 0 ? -i : i -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("i < 0 ? -i : i -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(() =>
                {
                    ++number[0];
                    int mask = number[0] >> 31;
                    number[0] = (number[0] + mask) ^ mask;
                }, 1000000
                    );
                Debug.LogFormat("(i + mask) ^ mask -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("(i + mask) ^ mask -> out:{0}, time:{1}", number[0], t.ToString(CultureInfo.InvariantCulture));

                number[0] = 0;
                t = Clock.BenchmarkTime(() =>
                {
                    ++number[0];
                    number[0] = (number[0] + (number[0] >> 31)) ^ (number[0] >> 31);
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
    }
}
