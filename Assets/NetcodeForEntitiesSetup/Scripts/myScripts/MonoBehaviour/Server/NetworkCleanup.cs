using Unity.Entities;
using UnityEngine;

public class NetworkCleanup : MonoBehaviour
{
    void Awake()
    {
        // Sprawia, ¿e ten obiekt nie zginie przy zmianie sceny
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        // To wykona siê zawsze, gdy zamkniesz grê, 
        // niezale¿nie od tego, na której scenie bêdziesz.
        var worlds = World.All;
        for (int i = worlds.Count - 1; i >= 0; i--)
        {
            var world = worlds[i];
            if (world.Name == "ServerWorld" || world.Name == "ClientWorld")
            {
                if (world.IsCreated)
                {
                    // Kluczowe: zatrzymujemy procesy i zwalniamy porty
                    world.EntityManager.CompleteAllTrackedJobs();
                    world.Dispose();
                    Debug.Log($"[Cleanup] Port released from {world.Name}");
                }
            }
        }
    }
}