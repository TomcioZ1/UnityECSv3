using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;

public class UIStatsDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI pingText;

    [Header("Settings")]
    public float updateInterval = 1.0f; // Czas w sekundach miêdzy odœwie¿eniami

    private EntityManager _entityManager;
    private World _clientWorld;
    private EntityQuery _statsQuery;
    private float _timer; // Licznik czasu

  

    void Update()
    {
        // 1. Obs³uga timera
        _timer -= Time.deltaTime;
        if (_timer > 0) return; // Jeœli czas jeszcze nie min¹³, przerywamy Update

        _timer = updateInterval; // Resetujemy timer

        // 2. Szukanie œwiata klienta
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            _clientWorld = null;
            foreach (var world in World.All)
            {
                if (world.IsClient() && !world.IsThinClient())
                {
                    _clientWorld = world;
                    _entityManager = _clientWorld.EntityManager;
                    _statsQuery = _entityManager.CreateEntityQuery(typeof(PerformanceStats));
                    Debug.Log($"[UI] Po³¹czono ze œwiatem: {world.Name}");
                    break;
                }
            }
            if (_clientWorld == null) return;
        }

        // 3. Pobieranie danych i aktualizacja UI (wykona siê raz na 'updateInterval')
        if (_statsQuery != null && _statsQuery.HasSingleton<PerformanceStats>())
        {
            var stats = _statsQuery.GetSingleton<PerformanceStats>();

            if (fpsText != null)
                fpsText.text = $"FPS: {Mathf.RoundToInt(stats.FPS)}";

            if (pingText != null)
                pingText.text = $"Ping: {Mathf.RoundToInt(stats.Ping)}";

            // Debug.Log("UI Updated"); // Mo¿esz odkomentowaæ, by sprawdziæ w konsoli jak rzadko siê pojawia
        }
    }
}