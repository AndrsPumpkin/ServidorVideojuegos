using Unity.Entities;
using UnityEngine;

namespace NetworkingLab.NFE
{
    public sealed class NfeBulletAuthoring : MonoBehaviour
    {
        private sealed class NfeBulletBaker : Baker<NfeBulletAuthoring>
        {
            public override void Bake(NfeBulletAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<NfeBulletTag>(entity);
                AddComponent<NfeBulletState>(entity);
            }
        }
    }
}
