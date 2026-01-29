using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct ProjectileComponent : IComponentData
{
    [GhostField(Quantization = 100)] public float3 Velocity;
    [GhostField] public double DeathTime;
    [GhostField] public Entity Owner;
    [GhostField] public int Damage;
}