using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.NetCode;
using Unity;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PunchHitSystem : ISystem
{
    private ComponentLookup<HealthComponent> _healthLookup;
    private ComponentLookup<HandAttackData> _attackDataLookup;
    private ComponentLookup<HandsOwner> _ownerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _healthLookup = state.GetComponentLookup<HealthComponent>(false);
        _attackDataLookup = state.GetComponentLookup<HandAttackData>(false);
        _ownerLookup = state.GetComponentLookup<HandsOwner>(true);
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _healthLookup.Update(ref state);
        _attackDataLookup.Update(ref state);
        _ownerLookup.Update(ref state);

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        state.Dependency = new PunchTriggerJob
        {
            HealthLookup = _healthLookup,
            AttackDataLookup = _attackDataLookup,
            OwnerLookup = _ownerLookup
        }.Schedule(simulation, state.Dependency);
    }
}

[BurstCompile]
struct PunchTriggerJob : ITriggerEventsJob
{
    public ComponentLookup<HealthComponent> HealthLookup;
    public ComponentLookup<HandAttackData> AttackDataLookup;
    [ReadOnly] public ComponentLookup<HandsOwner> OwnerLookup;

    public void Execute(TriggerEvent triggerEvent)
    {
        ProcessCollision(triggerEvent.EntityA, triggerEvent.EntityB);
        ProcessCollision(triggerEvent.EntityB, triggerEvent.EntityA);
    }

    private void ProcessCollision(Entity striker, Entity receiver)
    {
        // 1. Sprawdzamy czy striker to d³oñ i czy cel ma ¿ycie
        if (OwnerLookup.HasComponent(striker) && HealthLookup.HasComponent(receiver))
        {
            Entity ownerEntity = OwnerLookup[striker].Entity;

            // Nie bijemy samych siebie
            if (ownerEntity == receiver) return;

            // 2. Pobieramy dane ataku gracza, do którego nale¿y d³oñ
            if (AttackDataLookup.HasComponent(ownerEntity))
            {
                var attack = AttackDataLookup[ownerEntity];

                // 3. WARUNEK HITU: Musi byæ w fazie ataku, odpowiednim progresie i nie mieæ jeszcze zaliczonego hita
                if (attack.IsAttacking && attack.AttackProgress >= 0.6f && !attack.HasAppliedDamage)
                {
                    // Zadawanie obra¿eñ
                    var hp = HealthLookup[receiver];
                    hp.HealthPoints -= attack.AttackDamage;
                    hp.LastHitBy = ownerEntity;
                    HealthLookup[receiver] = hp;

                    // Blokujemy wielokrotne hity w tym samym zamachu
                    attack.HasAppliedDamage = true;
                    AttackDataLookup[ownerEntity] = attack;

                    // Log sukcesu na serwerze
                    //Unity.Debug.Log($"[SERVER] Hit! Gracz {ownerEntity.Index} uderzyl {receiver.Index}. Damage: {attack.AttackDamage}");
                }
            }
        }
    }
}