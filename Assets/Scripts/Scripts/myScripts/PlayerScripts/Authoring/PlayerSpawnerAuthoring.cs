using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    [DisallowMultipleComponent]
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Player;
        public List<Transform> SpawnPoints; // Tutaj przeci¹gasz 10 pustych obiektów z hierarchii

        class PlayerSpawnerAuthoringBaker : Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // Dodajemy g³ówny komponent
                AddComponent(entity, new PlayerSpawner
                {
                    Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic),
                    NextSpawnIndex = 0
                });

                // Dodajemy bufor i wype³niamy go pozycjami
                var buffer = AddBuffer<SpawnPointElement>(entity);
                foreach (var sp in authoring.SpawnPoints)
                {
                    if (sp != null)
                    {
                        buffer.Add(new SpawnPointElement { Position = sp.position });
                    }
                }
            }
        }
    }

    // Element bufora przechowuj¹cy pozycjê spawnu
    public struct SpawnPointElement : IBufferElementData
    {
        public float3 Position;
    }

    public struct PlayerSpawner : IComponentData
    {
        public Entity Player;
        public int NextSpawnIndex; // Opcjonalnie: do spawnowania po kolei (Round Robin)
    }



}