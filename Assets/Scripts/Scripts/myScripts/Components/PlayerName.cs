using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using TMPro;

[GhostComponent]
public struct PlayerName : IComponentData
{
    [GhostField]
    public FixedString64Bytes Value;
}

public struct IsDestroyedTag : IComponentData { }