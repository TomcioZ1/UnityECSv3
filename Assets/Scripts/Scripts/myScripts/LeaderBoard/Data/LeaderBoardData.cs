using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[GhostComponent]
/*[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]*/
[StructLayout(LayoutKind.Sequential)]
public struct LeaderboardElement : IBufferElementData
{
    [GhostField] public FixedString32Bytes PlayerName;
    [GhostField] public int Kills;
    [GhostField] public int Deaths;
}

public struct KillEvent : IComponentData
{
    public FixedString64Bytes KillerName;
}