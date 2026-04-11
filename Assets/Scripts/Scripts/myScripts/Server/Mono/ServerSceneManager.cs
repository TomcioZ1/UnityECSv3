using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using System.Linq;
using Unity.NetCode;

public class ServerSceneManager : MonoBehaviour
{
    void Update()
    {
        // Szukamy œwiata serwera
        var serverWorld = World.All.FirstOrDefault(w => w.IsServer());
        if (serverWorld == null) return;

        var em = serverWorld.EntityManager;

        // Sprawdzamy, czy w œwiecie serwera istnieje encja z naszym tagiem
        var query = em.CreateEntityQuery(typeof(QuitToServerSceneTag));

        if (!query.IsEmpty)
        {
            Debug.Log("Sygna³ odebrany w MonoBehaviour. £adujê ServerScene...");

            // Usuwamy tag, ¿eby nie ³adowaæ sceny w nieskoñczonoœæ
            em.DestroyEntity(query);

            // Klasyczne ³adowanie sceny Unity
            SceneManager.LoadScene("ServerScene");
        }
    }
}