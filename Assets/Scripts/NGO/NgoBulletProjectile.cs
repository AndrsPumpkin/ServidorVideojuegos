using Unity.Netcode;
using UnityEngine;

namespace NetworkingLab.NGO
{
    public sealed class NgoBulletProjectile : NetworkBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 12f;
        [SerializeField, Min(0f)] private float lifetime = 2f;
        [SerializeField, Min(0.01f)] private float hitRadius = 0.7f;
        [SerializeField] private Vector2 horizontalBounds = new(10.5f, 7.5f);

        private int ownerNumber;
        private float age;

        public void Configure(int owner)
        {
            ownerNumber = owner;
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned) return;

            float deltaTime = Time.deltaTime;
            age += deltaTime;
            transform.position += transform.forward * (speed * deltaTime);

            Vector3 position = transform.position;
            if (age >= lifetime ||
                Mathf.Abs(position.x) > horizontalBounds.x ||
                Mathf.Abs(position.z) > horizontalBounds.y)
            {
                Despawn();
                return;
            }

            Collider[] overlaps = Physics.OverlapSphere(position, hitRadius);
            for (int index = 0; index < overlaps.Length; index++)
            {
                NgoPlayerNetwork victim = overlaps[index].GetComponentInParent<NgoPlayerNetwork>();
                if (victim == null || !victim.IsSpawned) continue;
                if (victim.PlayerNumber == ownerNumber) continue;
                victim.ServerRegisterHit();
                Despawn();
                return;
            }
        }

        private void Despawn()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}
