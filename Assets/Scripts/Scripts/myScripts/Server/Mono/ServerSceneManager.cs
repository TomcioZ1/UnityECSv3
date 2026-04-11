using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;
using System.Collections;

public class ServerSceneManager : MonoBehaviour
{
    private bool _isRestarting = false;

    void Update()
    {
        if (_isRestarting) return;

        World serverWorld = null;
        foreach (var w in World.All)
        {
            if (w.IsServer())
            {
                serverWorld = w;
                break;
            }
        }
        if (serverWorld == null) return;

        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(QuitToServerSceneTag));

        if (!query.IsEmpty)
        {
            _isRestarting = true;
            Debug.Log("[SERVER] Sygna³ odebrany. Restartujê serwer...");
            StartCoroutine(RestartServer());
        }
    }

    IEnumerator RestartServer()
    {
        // Krok 1: Zniszcz encje z tagiem (¿eby systemy ECS nie reagowa³y w tym frame)
        foreach (var w in World.All)
        {
            if (w.IsServer())
            {
                var em = w.EntityManager;
                var query = em.CreateEntityQuery(typeof(QuitToServerSceneTag));
                if (!query.IsEmpty)
                    em.DestroyEntity(query);
                break;
            }
        }

        // Krok 2: Czekaj jeden frame — pozwól ECS dokoñczyæ bie¿¹cy tick
        yield return null;

        // Krok 3: Zniszcz wszystkie œwiaty ECS
        Debug.Log("[SERVER] Niszczê wszystkie œwiaty ECS...");
        var worlds = new System.Collections.Generic.List<World>();
        foreach (var w in World.All)
            worlds.Add(w);

        foreach (var w in worlds)
        {
            if (w.IsCreated)
            {
                Debug.Log($"[SERVER] Disposing: {w.Name}");
                w.Dispose();
            }
        }

        World.DefaultGameObjectInjectionWorld = null;

        // Krok 4: Jeszcze jeden frame przed ³adowaniem sceny
        yield return null;

        Debug.Log("[SERVER] Prze³adowujê ServerScene...");
        SceneManager.LoadScene("ServerScene", LoadSceneMode.Single);
        // ServerAutoInitializer.Start() odpali siê automatycznie na nowej scenie
    }
}