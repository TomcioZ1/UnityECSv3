using Unity.Entities;
using Unity.NetCode;

[GhostComponent]
public struct HealthComponent : IComponentData
{
    [GhostField] public int HealthPoints;
    [GhostField] public int MaxHealthPoints;
    [GhostField] public Entity LastHitBy;
}
