using Unity.Entities;
using Unity.NetCode;
public struct HandsComponent : IComponentData
{
    [GhostField] public Entity LeftHand;
    [GhostField] public Entity RightHand;
    [GhostField] public HandState State;
    [GhostField] public float AnimationTimer;
}

public enum HandState { Idle, LeftPunch, RightPunch }

public struct PunchingHand : IComponentData
{
    public float Damage;
    public float Reach; // Jak daleko wysuwa siê rêka
}