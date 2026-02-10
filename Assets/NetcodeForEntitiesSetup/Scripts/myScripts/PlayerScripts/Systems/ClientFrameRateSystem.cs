/*using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ClientFrameRateSystem : SystemBase
{
    protected override void OnCreate()
    {
        int targetFPS = 30; // Ustaw docelow¹ liczbê klatek na sekundê
        // To wykona siê tylko w œwiecie klienta
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;
        Debug.Log("Client World detected: FPS set to: " + targetFPS);
    }

    protected override void OnUpdate() { }
}*/