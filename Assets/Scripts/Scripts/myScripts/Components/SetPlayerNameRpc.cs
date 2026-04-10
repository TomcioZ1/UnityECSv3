using Unity.NetCode;
using Unity.Collections;

public struct SetPlayerNameRpc : IRpcCommand
{
    public FixedString64Bytes Name;
}
