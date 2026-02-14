using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct ProjectileComponent : IComponentData
{
    [GhostField] /*(Quantization = 100)] */public float3 Velocity;
    [GhostField] public double SpawnTime;
    [GhostField] public double DeathTime;
    [GhostField] public Entity Owner;
    [GhostField] public int Damage;
    [GhostField] public float3 TargetPosition;
}

public struct  BulletSpawnTimer : IComponentData
{
    public double Timer;
    public double AddedTime;
}

public struct ProjectileComponentNoScary : IComponentData
{
    public float3 Velocity;
    public Entity Owner;
 
}