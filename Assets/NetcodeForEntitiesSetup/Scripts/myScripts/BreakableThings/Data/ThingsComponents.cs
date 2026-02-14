using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent]
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


public struct SyncDestroyedGhostsRPC : IRpcCommand
{
    // U¿ywamy FixedList, aby przes³aæ listê ID. 128 elementów to bezpieczny limit dla RPC.
    public FixedList128Bytes<int> GhostIds;
}

[GhostComponent]
public struct GhostState : IComponentData
{
    [GhostField] public bool IsDestroyed;
}