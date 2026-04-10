using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))] // Lub domyœlna grupa symulacji
partial struct GameStopSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<TimeToStopTheGame>();

        Entity entity = state.EntityManager.CreateEntity();

        // 2. Dodajemy komponent z domyœlnymi lub startowymi wartoœciami
        // Ustawiamy bardzo wysok¹ wartoœæ, ¿eby gra nie skoñczy³a siê natychmiast,
        // dopóki inna logika nie ustawi w³aœciwego czasu.
        state.EntityManager.AddComponentData(entity, new TimeToStopTheGame
        {
            ExactTimeOfGameStop = Time.time + 300f
        });

        
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (timer, entity) in SystemAPI.Query<RefRO<TimeToStopTheGame>>().WithEntityAccess())
        {
            // Sprawdzenie czy czas serwerowy przekroczy³ czas zakoñczenia
            if (SystemAPI.Time.ElapsedTime >= timer.ValueRO.ExactTimeOfGameStop)
            {
                Debug.Log("Game stopped! Time to clean up entities.");
            }
        }

        
    }

   
}