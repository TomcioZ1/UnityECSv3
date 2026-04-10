using Unity.Entities;
using UnityEngine;

public class WeaponSpawerAuthoring : MonoBehaviour
{
    public GameObject ShotgunPrefab;
    public GameObject AK47Prefab;
    public GameObject AWPPrefab;
    public GameObject RocketLauncherPrefab;
    public GameObject MP5Prefab;


    public class Baker : Baker<WeaponSpawerAuthoring>
    {
        public override void Bake(WeaponSpawerAuthoring authoring)
        {
            // U¿ywamy None dla Singletona, bo on sam nie musi mieæ pozycji w œwiecie
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new WeaponResources
            {
                Shotgun = GetEntity(authoring.ShotgunPrefab, TransformUsageFlags.Dynamic),
                ak47 = GetEntity(authoring.AK47Prefab, TransformUsageFlags.Dynamic),
                mp5 = GetEntity(authoring.MP5Prefab, TransformUsageFlags.Dynamic),
                awp = GetEntity(authoring.AWPPrefab, TransformUsageFlags.Dynamic),
                PKM = GetEntity(authoring.RocketLauncherPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}