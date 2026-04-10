using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using System.Collections.Generic;

// Struktura pomocnicza, aby widzieæ suwak w Inspektorze
[System.Serializable]
public struct SoundSetting
{
    public string name; // Dla lepszej czytelnoœci w edytorze
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
}

public class AudioSystemBridge : MonoBehaviour
{
    [Header("Ustawienia")]
    public AudioSource soundPrefab;

    // Zmieniamy AudioClip[] na listê ustawieñ z g³oœnoœci¹
    public List<SoundSetting> soundSettings = new List<SoundSetting>();

    private Dictionary<int, AudioSource> activeLoops = new Dictionary<int, AudioSource>();
    private HashSet<int> receivedThisFrame = new HashSet<int>();

    private EntityManager entityManager;
    private EntityQuery soundQuery;
    private bool isInitialized = false;

    private bool TryInitialize()
    {
        World clientWorld = null;
        foreach (var world in World.All)
        {
            if (world.IsClient()) { clientWorld = world; break; }
        }

        if (clientWorld == null) return false;

        entityManager = clientWorld.EntityManager;
        soundQuery = entityManager.CreateEntityQuery(typeof(PlaySoundRequest));
        isInitialized = true;
        return true;
    }

    void Update()
    {
        if (!isInitialized && !TryInitialize()) return;

        receivedThisFrame.Clear();

        var entities = soundQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var request = entityManager.GetComponentData<PlaySoundRequest>(entity);

            // Sprawdzamy czy ID mieœci siê w zakresie naszej listy
            if (request.SoundID < 0 || request.SoundID >= soundSettings.Count)
            {
                entityManager.DestroyEntity(entity);
                continue;
            }

            if (request.IsLoop)
            {
                HandleLoopingSound(request);
                receivedThisFrame.Add(request.SoundID);
            }
            else
            {
                PlayOneShot(request);
            }

            entityManager.DestroyEntity(entity);
        }

        ManageLoops();
    }

    private void HandleLoopingSound(PlaySoundRequest request)
    {
        var settings = soundSettings[request.SoundID];

        if (!activeLoops.ContainsKey(request.SoundID))
        {
            // Tworzymy now¹ pêtlê
            AudioSource source = Instantiate(soundPrefab, request.Position, Quaternion.identity);
            source.clip = settings.clip;
            source.volume = settings.volume; // <--- Tu ustawiamy g³oœnoœæ z suwaka
            source.loop = true;
            source.Play();
            activeLoops.Add(request.SoundID, source);
        }
        else
        {
            // Aktualizujemy istniej¹c¹ pêtlê
            var source = activeLoops[request.SoundID];
            source.transform.position = request.Position;

            // Na wypadek, gdybyœ zmieni³ g³oœnoœæ w trakcie gry w Inspektorze:
            source.volume = settings.volume;

            if (!source.isPlaying)
                source.UnPause();
        }
    }

    private void ManageLoops()
    {
        // Sprawdzamy, które pêtle powinny przestaæ graæ
        // U¿ywamy ToArray, aby móc bezpiecznie modyfikowaæ kolekcjê podczas iteracji
        var keys = new List<int>(activeLoops.Keys);
        foreach (var soundID in keys)
        {
            if (!receivedThisFrame.Contains(soundID))
            {
                if (activeLoops[soundID].isPlaying)
                {
                    activeLoops[soundID].Pause();
                }
            }
        }
    }

    private void PlayOneShot(PlaySoundRequest request)
    {
        if (soundPrefab == null) return;

        var settings = soundSettings[request.SoundID];
        if (settings.clip == null) return;

        AudioSource source = Instantiate(soundPrefab, request.Position, Quaternion.identity);
        source.clip = settings.clip;
        source.volume = settings.volume; // <--- Tu ustawiamy g³oœnoœæ z suwaka
        source.loop = false;
        source.clip = settings.clip;
        source.Play();

        Destroy(source.gameObject, settings.clip.length);
    }

    private void OnDestroy()
    {
        foreach (var source in activeLoops.Values)
        {
            if (source != null) Destroy(source.gameObject);
        }
        activeLoops.Clear();
    }
}