using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using System;

public class TimeToStopGmaeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject LeaderBoardPanel;
    [SerializeField] private GameObject QuitButton;
    private World _clientWorld;
    private bool _initialized = false;
    private float ExactStopTime = -1;

    void Update()
    {
        // 1. Inicjalizacja: Szukamy œwiata i encji a¿ siê pojawi¹
        if (!_initialized)
        {
            InitializeECS();
            return;
        }

        // 2. Pobieramy AKTUALNY czas sieciowy (NIE lokalny Time.time!)
        if (_clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkTime)).TryGetSingleton<NetworkTime>(out var networkTime))
        {
            double remainingTime = ExactStopTime - Time.time;

            if (remainingTime < 0) remainingTime = 0;

            // 3. Formatowanie
            TimeSpan t = TimeSpan.FromSeconds(remainingTime);
            timerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);

            if(remainingTime == 0)
            {
                StopTheGame();
            }
        }
    }

    private void InitializeECS()
    {
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            _clientWorld = GetClientWorld();
            if (_clientWorld == null) return;
        }

        // Sprawdzamy czy encja z czasem ju¿ dotar³a z serwera
        var query = _clientWorld.EntityManager.CreateEntityQuery(typeof(TimeToStopTheGame));
        if (!query.IsEmptyIgnoreFilter)
        {
            ExactStopTime = query.GetSingleton<TimeToStopTheGame>().ExactTimeOfGameStop;
            _initialized = true;
            Debug.Log($"[UI] Zainicjowano! Koniec gry o: {ExactStopTime}");
        }
    }

    private World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient()) return world;
        }
        return null;
    }



    void StopTheGame()
    {
        LeaderBoardPanel.SetActive(true);
        QuitButton.SetActive(true);
    }





}