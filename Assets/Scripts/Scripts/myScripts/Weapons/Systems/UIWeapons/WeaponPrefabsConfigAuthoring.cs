using Unity.Entities;
using UnityEngine;

// Komponent ECS przechowuj¹cy prefaby


// Skrypt MonoBehaviour do podpiêcia w Inspektorze (np. na pustym obiekcie "GameManager")
public class WeaponPrefabsConfigAuthoring : MonoBehaviour
{
    public GameObject MP5;
    public GameObject Shotgun;
    public GameObject AK47;
    public GameObject AWP;
    public GameObject RocketLauncher;

    class Baker : Baker<WeaponPrefabsConfigAuthoring>
    {
        public override void Bake(WeaponPrefabsConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new WeaponUIPrefabsConfig
            {
                // Zwróæ uwagê na TransformUsageFlags.Dynamic, bo bronie bêd¹ mia³y fizykê i bêd¹ siê poruszaæ
                MP5Prefab = GetEntity(authoring.MP5, TransformUsageFlags.Dynamic),
                ShotgunPrefab = GetEntity(authoring.Shotgun, TransformUsageFlags.Dynamic),
                AK47Prefab = GetEntity(authoring.AK47, TransformUsageFlags.Dynamic),
                AWPPrefab = GetEntity(authoring.AWP, TransformUsageFlags.Dynamic),
                RocketLauncherPrefab = GetEntity(authoring.RocketLauncher, TransformUsageFlags.Dynamic)
            });
        }
    }
}