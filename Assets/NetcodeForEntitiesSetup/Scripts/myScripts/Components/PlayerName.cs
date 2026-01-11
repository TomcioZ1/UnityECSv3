using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

public struct PlayerName : IComponentData
{
    [GhostField]
    public FixedString64Bytes Value;
}

