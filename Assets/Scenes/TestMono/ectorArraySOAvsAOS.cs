using UnityEngine;
using Unity.Mathematics;
using System.Diagnostics;
using RND = UnityEngine.Random;
using DBG = UnityEngine.Debug;

public class VectorArraySOAvsAOS : MonoBehaviour
{
    private const int ElementCount = 1000000;
    private const int SampleSize = 1000;

    private float[] x, y, z;
    private float3[] vectors;
    private Stopwatch stopwatch = new Stopwatch();

    private long totalSoATicks = 0;
    private long totalAoSTicks = 0;
    private int frameCounter = 0;

    void Start()
    {
        x = new float[ElementCount];
        y = new float[ElementCount];
        z = new float[ElementCount];
        vectors = new float3[ElementCount];

        for (int i = 0; i < ElementCount; i++)
        {
            x[i] = RND.value * 100;
            y[i] = RND.value * 100;
            z[i] = RND.value * 100;
            vectors[i] = new float3(x[i], y[i], z[i]);
        }
    }

    void Update()
    {
        stopwatch.Restart();
        for (int i = 0; i < x.Length; i++)
        {
            x[i] *= 2f;
            y[i] *= 2f;
            z[i] *= 2f;
        }
        stopwatch.Stop();
        totalSoATicks += stopwatch.Elapsed.Ticks;

        stopwatch.Restart();
        for (int i = 0; i < vectors.Length; i++)
        {
            vectors[i] *= 2f;
        }
        stopwatch.Stop();
        totalAoSTicks += stopwatch.Elapsed.Ticks;

        frameCounter++;

        if (frameCounter >= SampleSize)
        {
            long avgSoA = totalSoATicks / SampleSize;
            long avgAoS = totalAoSTicks / SampleSize;
            float ratio = (float)avgAoS / avgSoA;

            DBG.Log($"<b>[Average Report: {SampleSize} frames]</b>\n" +
                    $"Average SOA: <color=green>{avgSoA}</color> ticks\n" +
                    $"Average AOS: <color=yellow>{avgAoS}</color> ticks\n" +
                    $"Performance Ratio: <color=white>{ratio:F2}x</color> faster in SOA");

            totalSoATicks = 0;
            totalAoSTicks = 0;
            frameCounter = 0;
        }
    }
}