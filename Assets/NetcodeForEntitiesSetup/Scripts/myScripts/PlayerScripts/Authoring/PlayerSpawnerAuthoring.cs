using Unity.Entities;
using UnityEngine;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    /// <summary>
    /// Component data that identifies a cube spawner and gives access to the cube prefab.
    /// </summary>
    public struct PlayerSpawner : IComponentData
    {
        /// <summary>
        /// The Cube prefab converted to an entity.
        /// </summary>
        public Entity Player;
    }

    /// <summary>
    /// Baker that transforms our cube prefab into an entity and creates a spawner entity.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {
        /// <summary>
        /// The cube prefab to spawn.
        /// </summary>
        public GameObject Player;

        class PlayerSpawnerAuthoringBaker : Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring authoring)
            {
                PlayerSpawner component = default(PlayerSpawner);
                component.Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
