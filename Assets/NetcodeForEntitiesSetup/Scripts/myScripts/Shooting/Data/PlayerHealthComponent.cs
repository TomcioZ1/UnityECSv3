using Unity.Entities;
using Unity.NetCode;

[GhostComponent]
public struct HealthComponent : IComponentData
{
    [GhostField] public int HealthPoints;
    [GhostField] public Entity LastHitBy;
}
