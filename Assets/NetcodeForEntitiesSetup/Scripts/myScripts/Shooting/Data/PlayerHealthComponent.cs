using Unity.Entities;
using Unity.NetCode;

public struct HealthComponent : IComponentData
{
    [GhostField] public int HealthPoints;
}
