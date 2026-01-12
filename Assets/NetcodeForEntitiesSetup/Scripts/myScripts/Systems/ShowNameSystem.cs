/*using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ShowNameSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // System uruchomi sie tylko jesli istnieje PlayerName
        state.RequireForUpdate<PlayerName>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var playerName in SystemAPI.Query<RefRO<PlayerName>>())
        {
            Debug.Log("Player name: " + playerName.ValueRO.Value.ToString());
        }
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}
*/