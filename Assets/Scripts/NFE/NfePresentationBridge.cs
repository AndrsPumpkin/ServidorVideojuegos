using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace NetworkingLab.NFE
{
    /// <summary>
    /// Converts replicated ECS state into deliberately simple local GameObject visuals.
    /// The GameObjects are presentation only; movement and ownership remain in NFE.
    /// </summary>
    public sealed class NfePresentationBridge : MonoBehaviour
    {
        private readonly struct Visual
        {
            public readonly GameObject Root;
            public readonly Transform Label;

            public Visual(GameObject root, Transform label)
            {
                Root = root;
                Label = label;
            }
        }

        private readonly Dictionary<Entity, Visual> visuals = new();

        [SerializeField] private GameObject playerVisualPrefab;

        public void Configure(GameObject visualPrefab)
        {
            playerVisualPrefab = visualPrefab;
        }

        private void LateUpdate()
        {
            World client = ClientServerBootstrap.ClientWorld;
            if (client == null || !client.IsCreated)
            {
                return;
            }

            EntityManager entityManager = client.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NfePlayerTag>(),
                ComponentType.ReadOnly<NfePlayerState>());

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            HashSet<Entity> live = new HashSet<Entity>();
            foreach (Entity entity in entities)
            {
                live.Add(entity);
                NfePlayerState state = entityManager.GetComponentData<NfePlayerState>(entity);

                if (!visuals.TryGetValue(entity, out Visual visual))
                {
                    visual = CreateVisual(state.PlayerNumber);
                    visuals.Add(entity, visual);
                }

                visual.Root.transform.position = new Vector3(state.Position.x, 0.75f, state.Position.y);
                visual.Root.transform.rotation = Quaternion.Euler(0f, state.RotationY, 0f);
                if (visual.Label != null)
                {
                    visual.Label.rotation = Quaternion.Euler(52f, 0f, 0f);
                }
            }

            entities.Dispose();
            query.Dispose();

            List<Entity> removed = new List<Entity>();
            foreach (KeyValuePair<Entity, Visual> pair in visuals)
            {
                if (!live.Contains(pair.Key))
                {
                    Destroy(pair.Value.Root);
                    removed.Add(pair.Key);
                }
            }

            foreach (Entity entity in removed)
            {
                visuals.Remove(entity);
            }
        }

        private void OnDestroy()
        {
            foreach (Visual visual in visuals.Values)
            {
                if (visual.Root != null)
                {
                    Destroy(visual.Root);
                }
            }

            visuals.Clear();
        }

        private Visual CreateVisual(int playerNumber)
        {
            if (playerVisualPrefab != null)
            {
                return CreateVisualFromPrefab(playerNumber);
            }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = $"NFE Player {playerNumber} Visual";
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().material.color = PlayerColor(playerNumber);

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(visual.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = $"Player {playerNumber}";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = 0.08f;
            label.color = Color.white;
            return new Visual(visual, labelObject.transform);
        }

        private Visual CreateVisualFromPrefab(int playerNumber)
        {
            GameObject visual = Instantiate(playerVisualPrefab);
            visual.name = $"NFE Player {playerNumber} Visual";
            visual.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }

            TextMesh label = visual.GetComponentInChildren<TextMesh>(true);
            if (label != null)
            {
                label.text = $"Player {playerNumber}";
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (label != null && renderer.gameObject == label.gameObject)
                {
                    continue;
                }

                renderer.material.color = PlayerColor(playerNumber);
            }

            return new Visual(visual, label != null ? label.transform : null);
        }

        private static Color PlayerColor(int number)
        {
            Color[] colors =
            {
                new(0.1f, 0.55f, 1f),
                new(1f, 0.25f, 0.2f),
                new(0.25f, 0.85f, 0.35f),
                new(1f, 0.72f, 0.12f)
            };
            return colors[(Mathf.Max(1, number) - 1) % colors.Length];
        }
    }
}
