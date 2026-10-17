using Unity.Entities;
using UnityEngine;

namespace NetworkingLab.NFE
{
    public sealed class NfeBulletSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject bulletPrefab;

        public void Configure(GameObject prefab)
        {
            bulletPrefab = prefab;
        }

        private sealed class NfeBulletSpawnerBaker : Baker<NfeBulletSpawnerAuthoring>
        {
            public override void Bake(NfeBulletSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NfeBulletSpawner
                {
                    BulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
