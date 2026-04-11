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




[GhostComponent]
public struct GhostState : IComponentData
{
    [GhostField] public bool IsDestroyed;
}


public struct DropWeapon : IComponentData
{
    //public Entity DropWeaponPrefab;
    public int DropChance;
}

public struct WeaponUIPrefabsConfig : IComponentData
{
    public Entity MP5Prefab;
    public Entity ShotgunPrefab;
    public Entity AK47Prefab;
    public Entity AWPPrefab;
    public Entity RocketLauncherPrefab;
}