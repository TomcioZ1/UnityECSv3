using Unity.Entities;

public struct PerformanceStats : IComponentData
{
    public float FPS;
    public float Ping;
}