using Unity.Entities;
using Unity.Mathematics;

// Dane na graczu
public struct FootprintSpawner : IComponentData
{
    public Entity FootprintPrefab;
}

// Dane œladu (do usuwania po czasie)
public struct FootprintLifeTime : IComponentData
{
    public float Value;
    public float MaxValue;
}

public struct PlayerFootprintState : IComponentData
{
    public float3 LastSpawnPosition;
    public bool LeftFoot;
    public bool IsInitialized; // Aby nie spawnowaæ œladu w (0,0,0) na starcie
    public float distanceBetweenLegs;
    public float distanceBetweenSteps;

}