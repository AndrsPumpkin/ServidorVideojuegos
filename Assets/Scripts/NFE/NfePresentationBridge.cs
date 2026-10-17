using System.Collections;
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
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color HitEmission = new Color(1f, 0f, 0f) * 3f;
        private const string EmissionKeyword = "_EMISSION";
        private const float FlashDuration = 0.15f;

        private readonly struct Visual
        {
            public readonly GameObject Root;
            public readonly Transform Label;
            public readonly Renderer[] Renderers;

            public Visual(GameObject root, Transform label, Renderer[] renderers)
            {
                Root = root;
                Label = label;
                Renderers = renderers;
            }
        }

        public static NfePresentationBridge Instance { get; private set; }

        private readonly Dictionary<Entity, Visual> playerVisuals = new();
        private readonly Dictionary<Entity, GameObject> bulletVisuals = new();
        private readonly Dictionary<int, Renderer[]> playerRenderersByNumber = new();
        private readonly Dictionary<int, Coroutine> flashRoutines = new();

        [SerializeField] private GameObject playerVisualPrefab;
        [SerializeField] private GameObject bulletVisualPrefab;

        public void Configure(GameObject playerVisual, GameObject bulletVisual = null)
        {
            playerVisualPrefab = playerVisual;
            bulletVisualPrefab = bulletVisual;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void LateUpdate()
        {
            World client = ClientServerBootstrap.ClientWorld;
            if (client == null || !client.IsCreated)
            {
                return;
            }

            EntityManager entityManager = client.EntityManager;
            UpdatePlayers(entityManager);
            UpdateBullets(entityManager);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            foreach (Visual visual in playerVisuals.Values)
            {
                if (visual.Root != null)
                {
                    Destroy(visual.Root);
                }
            }

            foreach (GameObject bullet in bulletVisuals.Values)
            {
                if (bullet != null)
                {
                    Destroy(bullet);
                }
            }

            playerVisuals.Clear();
            bulletVisuals.Clear();
            playerRenderersByNumber.Clear();
        }

        public static void RequestHitFlash(int playerNumber)
        {
            if (Instance != null)
            {
                Instance.FlashHit(playerNumber);
            }
        }

        private void FlashHit(int playerNumber)
        {
            if (flashRoutines.ContainsKey(playerNumber)) return;
            if (!playerRenderersByNumber.TryGetValue(playerNumber, out Renderer[] renderers) || renderers.Length == 0)
            {
                return;
            }

            flashRoutines[playerNumber] = StartCoroutine(FlashRoutine(playerNumber, renderers));
        }

        private IEnumerator FlashRoutine(int playerNumber, Renderer[] renderers)
        {
            List<(Material material, Color color, bool emission)> saved = new();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                Material material = renderer.material;
                saved.Add((material, material.GetColor(EmissionColorId), material.IsKeywordEnabled(EmissionKeyword)));
                material.EnableKeyword(EmissionKeyword);
                material.SetColor(EmissionColorId, HitEmission);
            }

            yield return new WaitForSeconds(FlashDuration);

            foreach ((Material material, Color color, bool emission) in saved)
            {
                if (material == null) continue;
                material.SetColor(EmissionColorId, color);
                if (!emission) material.DisableKeyword(EmissionKeyword);
            }

            flashRoutines.Remove(playerNumber);
        }

        private void UpdatePlayers(EntityManager entityManager)
        {
            EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NfePlayerTag>(),
                ComponentType.ReadOnly<NfePlayerState>());

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            HashSet<Entity> live = new HashSet<Entity>();
            foreach (Entity entity in entities)
            {
                live.Add(entity);
                NfePlayerState state = entityManager.GetComponentData<NfePlayerState>(entity);

                if (!playerVisuals.TryGetValue(entity, out Visual visual))
                {
                    visual = CreatePlayerVisual(state.PlayerNumber);
                    playerVisuals.Add(entity, visual);
                    playerRenderersByNumber[state.PlayerNumber] = visual.Renderers;
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
            foreach (KeyValuePair<Entity, Visual> pair in playerVisuals)
            {
                if (live.Contains(pair.Key)) continue;
                Destroy(pair.Value.Root);
                removed.Add(pair.Key);
            }

            foreach (Entity entity in removed)
            {
                playerVisuals.Remove(entity);
            }
        }

        private void UpdateBullets(EntityManager entityManager)
        {
            EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NfeBulletTag>(),
                ComponentType.ReadOnly<NfeBulletState>());

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            HashSet<Entity> live = new HashSet<Entity>();
            foreach (Entity entity in entities)
            {
                live.Add(entity);
                NfeBulletState state = entityManager.GetComponentData<NfeBulletState>(entity);

                if (!bulletVisuals.TryGetValue(entity, out GameObject visual))
                {
                    visual = CreateBulletVisual();
                    bulletVisuals.Add(entity, visual);
                }

                visual.transform.position = new Vector3(state.Position.x, state.Position.y, state.Position.z);
                Vector3 velocity = new Vector3(state.Velocity.x, state.Velocity.y, state.Velocity.z);
                if (velocity.sqrMagnitude > 0.0001f)
                {
                    visual.transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
                }
            }

            entities.Dispose();
            query.Dispose();

            List<Entity> removed = new List<Entity>();
            foreach (KeyValuePair<Entity, GameObject> pair in bulletVisuals)
            {
                if (live.Contains(pair.Key)) continue;
                Destroy(pair.Value);
                removed.Add(pair.Key);
            }

            foreach (Entity entity in removed)
            {
                bulletVisuals.Remove(entity);
            }
        }

        private Visual CreatePlayerVisual(int playerNumber)
        {
            if (playerVisualPrefab != null)
            {
                return CreatePlayerVisualFromPrefab(playerNumber);
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

            Renderer[] renderers = { visual.GetComponent<Renderer>() };
            return new Visual(visual, labelObject.transform, renderers);
        }

        private Visual CreatePlayerVisualFromPrefab(int playerNumber)
        {
            GameObject visual = Instantiate(playerVisualPrefab);
            visual.name = $"NFE Player {playerNumber} Visual";
            visual.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }

            Transform labelTransform = null;
            TextMesh label = visual.GetComponentInChildren<TextMesh>(true);
            if (label != null)
            {
                label.text = $"Player {playerNumber}";
                labelTransform = label.transform;
            }

            List<Renderer> renderers = new();
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (label != null && renderer.gameObject == label.gameObject) continue;
                renderer.material.color = PlayerColor(playerNumber);
                renderers.Add(renderer);
            }

            return new Visual(visual, labelTransform, renderers.ToArray());
        }

        private GameObject CreateBulletVisual()
        {
            GameObject visual;
            if (bulletVisualPrefab != null)
            {
                visual = Instantiate(bulletVisualPrefab);
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(collider);
                }
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(visual.GetComponent<Collider>());
                visual.transform.localScale = Vector3.one * 0.5f;
            }

            visual.name = "NFE Bullet Visual";
            return visual;
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