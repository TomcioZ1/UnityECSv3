/*using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 1. Wymuszamy dzia³anie w fazie renderowania (raz na klatkê obrazu)
[UpdateInGroup(typeof(PresentationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct ToggleDebug : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<LocalPauseState>())
        {
            state.EntityManager.CreateSingleton(new LocalPauseState { IsPaused = false });
        }
        if (!SystemAPI.HasSingleton<PressedKeyesComponent>())
        {
            state.EntityManager.CreateSingleton(new PressedKeyesComponent { EnterPressed = false, EscPressed = false });
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        // 1. Pobieramy dane wejœciowe (Read Only)
        var inputData = SystemAPI.GetSingleton<PressedKeyesComponent>();

        // 2. Pobieramy stan pauzy (Read-Write)
        var pauseState = SystemAPI.GetSingletonRW<LocalPauseState>();

        // 3. Logika przypisania
        // Jeœli EscPressed to true -> IsPaused bêdzie true. Jeœli false -> false.
        if (inputData.EscPressed || inputData.EnterPressed)
        {
            pauseState.ValueRW.IsPaused = true;
        }
        else
        {
            pauseState.ValueRW.IsPaused = false;
        }
    }
}*/