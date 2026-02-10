using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerLeaderboardUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. SprawdŸ czy mamy tabelê wyników na scenie
        if (!SystemAPI.TryGetSingletonEntity<LeaderboardTag>(out var leaderboardEntity)) return;

        var leaderboard = SystemAPI.GetBuffer<LeaderboardElement>(leaderboardEntity);
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        bool needsRefresh = false;

        // --- SEKCJA A: AUTOMATYCZNE DODAWANIE NOWYCH GRACZY ---
        // Szukamy encji z PlayerName, których nie ma jeszcze w tabeli
        foreach (var (playerName, entity) in SystemAPI.Query<RefRO<PlayerName>>()
                     .WithNone<InLeaderboardTag>()
                     .WithEntityAccess())
        {
            if (playerName.ValueRO.Value.Length == 0) continue;

            leaderboard.Add(new LeaderboardElement
            {
                PlayerName = playerName.ValueRO.Value.ToString(),
                Kills = 0,
                Deaths = 0
            });

            ecb.AddComponent<InLeaderboardTag>(entity);
            needsRefresh = true;
            //Debug.Log($"[LEADERBOARD] Dodano nowego gracza do rankingu: {playerName.ValueRO.Value}");
        }

        // --- SEKCJA B: OBS£UGA PUNKTÓW Z EVENTÓW ZABÓJSTW ---
        // Szukamy encji KillEvent stworzonych np. przez PlayerDeathSystem
        foreach (var (killEvent, eventEntity) in SystemAPI.Query<RefRO<KillEvent>>().WithEntityAccess())
        {
            FixedString64Bytes killer = killEvent.ValueRO.KillerName;

            for (int i = 0; i < leaderboard.Length; i++)
            {
                if (leaderboard[i].PlayerName == killer)
                {
                    var entry = leaderboard[i];
                    entry.Kills++;
                    leaderboard[i] = entry;

                    needsRefresh = true;
                    //Debug.Log($"[LEADERBOARD] Gracz {killer} zdoby³ punkt! (Suma: {entry.Kills})");
                    break;
                }
            }

            // Zniszcz encjê eventu, ¿eby nie naliczaæ punktów w kó³ko
            ecb.DestroyEntity(eventEntity);
        }

        // --- SEKCJA C: ODŒWIE¯ANIE ---
        if (needsRefresh)
        {
            SortLeaderboard(leaderboard);
            //LogLeaderboardStatus(leaderboard);
        }

        ecb.Playback(state.EntityManager);
    }

    // Metoda pomocnicza do rêcznego dodawania punktów (jeœli nadal chcesz jej u¿ywaæ)
    public void AddKill(ref SystemState state, FixedString32Bytes playerName)
    {
        if (!SystemAPI.TryGetSingletonEntity<LeaderboardTag>(out var leaderboardEntity)) return;
        var leaderboard = SystemAPI.GetBuffer<LeaderboardElement>(leaderboardEntity);

        for (int i = 0; i < leaderboard.Length; i++)
        {
            if (leaderboard[i].PlayerName == playerName)
            {
                var entry = leaderboard[i];
                entry.Kills++;
                leaderboard[i] = entry;
                SortLeaderboard(leaderboard);
                //LogLeaderboardStatus(leaderboard);
                return;
            }
        }
    }

    private void SortLeaderboard(DynamicBuffer<LeaderboardElement> buffer)
    {
        var array = buffer.AsNativeArray();
        for (int i = 0; i < array.Length - 1; i++)
        {
            for (int j = 0; j < array.Length - i - 1; j++)
            {
                if (array[j].Kills < array[j + 1].Kills)
                {
                    var temp = array[j];
                    array[j] = array[j + 1];
                    array[j + 1] = temp;
                }
            }
        }
    }

    private void LogLeaderboardStatus(DynamicBuffer<LeaderboardElement> buffer)
    {
        string status = "<color=cyan>--- RANKING SERWEROWY ---</color>\n";
        for (int i = 0; i < buffer.Length; i++)
        {
            status += $"{i + 1}. <b>{buffer[i].PlayerName}</b> | Kills: {buffer[i].Kills}\n";
        }
        Debug.Log(status);
    }
}

public struct InLeaderboardTag : IComponentData { }