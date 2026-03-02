using Unity.Entities;
using Unity.Rendering;

public struct HealthComponentHistory : IComponentData
{
    public int HealthPoints;
}

[MaterialProperty("_DissolveFloat")]
public struct DissolveProperty : IComponentData
{
    public float Value;
}