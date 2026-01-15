using Unity.Entities;
using Unity.NetCode;

public struct PlayerHealthComponent : IComponentData
{
    [GhostField] public int HealthPoints;
}
