using Unity.Entities;
using UnityEngine;

namespace NetworkingLab.NFE
{
    public sealed class NfePlayerSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;

        public void Configure(GameObject prefab)
        {
            playerPrefab = prefab;
        }

        private sealed class NfePlayerSpawnerBaker : Baker<NfePlayerSpawnerAuthoring>
        {
            public override void Bake(NfePlayerSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NfePlayerSpawner
                {
                    PlayerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
