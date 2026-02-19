using Unity.Entities;
using Unity.Mathematics;
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
    [GhostField] public bool AttackIsLeft;

    // To pole jest synchronizowane, aby klient móg³ np. wyœwietliæ 
    // informacjê o tym, ile obra¿eñ zadaje jego postaæ.
    [GhostField] public int AttackDamage;

    // To pole NIE jest [GhostField], bo s³u¿y tylko jako wewnêtrzna 
    // flaga logiczna serwera podczas zamachu.
    public bool HasAppliedDamage;

    // DŸwiek
    public bool HasPlayedSound;
}

[GhostComponent]
public struct BaseScale : IComponentData
{
    [GhostField] public float3 Value;
}

public struct PunchFiredEvent : IComponentData
{
    public float3 Position;
}