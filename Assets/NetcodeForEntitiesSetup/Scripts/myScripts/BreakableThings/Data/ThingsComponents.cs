using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct BoxComponent : IComponentData
{
    [GhostField] public bool isDestoryed;
    public float InitialScale;
    public float InitialY;
    public float MeshHeight;
    public float CenterOffset;
}

public struct DestroyConfig : IComponentData
{
    public Entity DropPrefab;
    public int Amount;
}
public struct DestroyedDrop : IComponentData
{
    public float3 Velocity;
    public float RemainingLife;
    public float MaxLife;     // Potrzebne do obliczania procentu ¿ycia
    public float BaseScale;
}

public struct AlreadyProcessedTag : ICleanupComponentData { }