using Unity.Entities;
using Unity.NetCode;

[GhostComponent] // <--- DODAJ TO
public struct ActiveHands : IComponentData
{
    [GhostField] public Entity LeftHandEntity;
    [GhostField] public Entity RightHandEntity;

    public Entity PrevLeftHand;
    public Entity PrevRightHand;
}

// Komponent na broni (¿eby wiedzia³a do kogo nale¿y)
public struct HandsOwner : IComponentData
{
    public Entity Entity;
}


public struct HandsResources : IComponentData
{
    public Entity LeftHand;
    public Entity RightHand;

}

public struct HandsSocket : IComponentData
{
    public Entity RightHandSocket; // Gniazdo w d³oni postaci
    public Entity LeftHandSocket; // Gniazdo w d³oni postaci
}


[GhostComponent]
public struct HandAttackData : IComponentData
{
    [GhostField] public float AttackProgress;
    [GhostField] public bool IsAttacking;
    [GhostField] public bool AttackIsLeft; // true = lewa, false = prawa
}