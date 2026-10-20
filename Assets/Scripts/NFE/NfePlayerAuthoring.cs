using Unity.Entities;
using UnityEngine;

namespace NetworkingLab.NFE
{
    public sealed class NfePlayerAuthoring : MonoBehaviour
    {
        private sealed class NfePlayerBaker : Baker<NfePlayerAuthoring>
        {
            public override void Bake(NfePlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<NfePlayerTag>(entity);
                AddComponent<NfePlayerState>(entity);
                AddComponent<NfePlayerInput>(entity);
                AddComponent<NfePlayerLives>(entity);
            }
        }
    }
}
