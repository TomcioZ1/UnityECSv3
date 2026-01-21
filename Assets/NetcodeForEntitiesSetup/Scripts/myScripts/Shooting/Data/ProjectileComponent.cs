using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct ProjectileComponent : IComponentData
{
    [GhostField] public float3 Velocity;
    [GhostField] public float Lifetime;
    [GhostField] public Entity Owner;
    [GhostField] public int Damage;
}