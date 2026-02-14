using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine; // Potrzebne do Debug.Log

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct SyncDestroyedGhostsClientSystem : ISystem
{
    // Zakomentuj Burst do debugowania, bo Debug.Log z tekstem tu nie zadzia³a
    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 1. Sprawdzamy przychodz¹ce RPC
        foreach (var (rpc, rpcEntity) in SystemAPI.Query<SyncDestroyedGhostsRPC>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            // --- DEBUG: WYPISYWANIE LISTY ---
            Debug.Log($"[Client] Odebrano RPC! Liczba ID w liœcie: {rpc.GhostIds.Length}");
            for (int i = 0; i < rpc.GhostIds.Length; i++)
            {
                Debug.Log($" -> ID w RPC[{i}]: {rpc.GhostIds[i]}");
            }
            // --------------------------------

            bool foundAny = false;

            // 2. Szukamy duchów
            foreach (var (ghostInst, ghostEntity) in SystemAPI.Query<RefRO<GhostInstance>>()
                         .WithNone<Disabled>()
                         .WithEntityAccess())
            {
                // Loguj ka¿dego ducha, którego widzisz (tylko do testów!)
                // Debug.Log($"[Client] Sprawdzam ducha na scenie: Entity {ghostEntity.Index}, GhostID: {ghostInst.ValueRO.ghostId}");

                if (IsGhostIdInList(in rpc.GhostIds, ghostInst.ValueRO.ghostId))
                {
                    ecb.AddComponent<Disabled>(ghostEntity);
                    Debug.Log($"[Client] SUKCES! Wy³¹czono ducha o ID: {ghostInst.ValueRO.ghostId}");
                    foundAny = true;
                }
            }

            if (!foundAny && rpc.GhostIds.Length > 0)
            {
                Debug.LogWarning("[Client] Odebrano ID zniszczonych duchów, ale nie znaleziono pasuj¹cych encji na scenie! (Mo¿e jeszcze siê nie zespawnowa³y?)");
            }

            ecb.DestroyEntity(rpcEntity);
        }
    }

    [BurstCompile]
    private static bool IsGhostIdInList(in FixedList128Bytes<int> list, int id)
    {
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == id) return true;
        }
        return false;
    }
}