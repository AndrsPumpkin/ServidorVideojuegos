using System.Collections.Generic;
using System.Linq;
using NetworkingLab.NFE;
using NetworkingLab.NGO;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using Unity.NetCode;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NetworkingLab.Editor
{
    public static class NetworkLabSceneBuilder
    {
        public const string NgoScenePath = "Assets/Scenes/NGOGameplay.unity";
        public const string NfeScenePath = "Assets/Scenes/NFEGameplay.unity";
        public const string NfeSubScenePath = "Assets/Scenes/NFE/NFEGameplaySubScene.unity";
        public const string NgoPrefabPath = "Assets/Prefabs/NGOPlayer.prefab";
        public const string NfePrefabPath = "Assets/Prefabs/NFEPlayer.prefab";
        public const string BulletPrefabPath = "Assets/Prefabs/Bullet.prefab";
        private const string InputPath = "Assets/Input/LocalPlayers.inputactions";

        [MenuItem("Networking Lab/Build NGO and NFE Scenes")]
        public static void BuildAll()
        {
            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (input == null)
            {
                LocalBaselineSceneBuilder.Build();
                input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            }

            GameObject ngoPrefab = BuildNgoPrefab(input);
            BuildNgoScene(ngoPrefab);
            GameObject nfePrefab = BuildNfePrefab();
            GameObject bulletPrefab = EnsureNfeBulletPrefab();
            BuildNfeSubScene(nfePrefab, bulletPrefab);
            BuildNfeScene(bulletPrefab);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NETWORK_LAB_SCENES_BUILT");
        }

        private static GameObject BuildNgoPrefab(InputActionAsset input)
        {
            AssetDatabase.DeleteAsset(NgoPrefabPath);
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "NGOPlayer";
            root.transform.position = new Vector3(0f, 0.75f, 0f);
            Renderer renderer = root.GetComponent<Renderer>();

            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = "Player";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = 0.08f;
            label.color = Color.white;

            root.AddComponent<NetworkObject>();
            NetworkTransform transform = root.AddComponent<NetworkTransform>();
            SerializedObject transformObject = new(transform);
            SerializedProperty authority = transformObject.FindProperty("AuthorityMode");
            if (authority != null)
            {
                authority.enumValueIndex = 0;
                transformObject.ApplyModifiedPropertiesWithoutUndo();
            }

            NgoPlayerNetwork player = root.AddComponent<NgoPlayerNetwork>();
            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
            player.Configure(input, "Player 1", renderer, label, 5f, new Vector2(10.5f, 7.5f), bulletPrefab);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, NgoPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildNgoScene(GameObject playerPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildEnvironment();

            GameObject managerObject = new("NetworkManager");
            UnityTransport transport = managerObject.AddComponent<UnityTransport>();
            NetworkManager manager = managerObject.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = playerPrefab,
                TickRate = 30,
                EnableSceneManagement = true
            };

            ConnectionPanel panel = BuildConnectionPanel("NGO — NETCODE FOR GAMEOBJECTS", "UDP 7979");
            NgoConnectionUI ui = panel.Canvas.gameObject.AddComponent<NgoConnectionUI>();
            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
            if (bulletPrefab == null)
            {
                Debug.LogError($"NGO bullet prefab not found at {BulletPrefabPath}.");
            }

            ui.Configure(panel.Address, panel.Port, panel.Status, panel.LocalPlayer, bulletPrefab);
            panel.Port.text = NgoConnectionUI.DefaultPort.ToString();
            AddButton(panel.ButtonParent, "Start Server", new Vector2(0f, 0f), ui.StartServer);
            AddButton(panel.ButtonParent, "Start Host", new Vector2(170f, 0f), ui.StartHost);
            AddButton(panel.ButtonParent, "Connect Client", new Vector2(340f, 0f), ui.ConnectClient);
            AddButton(panel.ButtonParent, "Shutdown", new Vector2(510f, 0f), ui.Shutdown);

            EditorSceneManager.SaveScene(scene, NgoScenePath);
        }

        private static GameObject BuildNfePrefab()
        {
            AssetDatabase.DeleteAsset(NfePrefabPath);
            GameObject root = new("NFEPlayer");
            root.AddComponent<NfePlayerAuthoring>();
            GhostAuthoringComponent ghost = root.AddComponent<GhostAuthoringComponent>();
            ghost.DefaultGhostMode = GhostMode.OwnerPredicted;
            ghost.HasOwner = true;
            ghost.SupportAutoCommandTarget = true;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, NfePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildNfeSubScene(GameObject playerPrefab, GameObject bulletPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject spawnerObject = new("NFE Player Spawner");
            spawnerObject.AddComponent<NfePlayerSpawnerAuthoring>().Configure(playerPrefab);
            GameObject bulletSpawnerObject = new("NFE Bullet Spawner");
            bulletSpawnerObject.AddComponent<NfeBulletSpawnerAuthoring>().Configure(bulletPrefab);
            EditorSceneManager.SaveScene(scene, NfeSubScenePath);
        }

        private static GameObject EnsureNfeBulletPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BulletPrefabPath);
            bool changed = false;

            if (root.GetComponent<NfeBulletAuthoring>() == null)
            {
                root.AddComponent<NfeBulletAuthoring>();
                changed = true;
            }

            if (root.GetComponent<LinkedEntityGroupAuthoring>() == null)
            {
                root.AddComponent<LinkedEntityGroupAuthoring>();
                changed = true;
            }

            if (root.GetComponent<GhostAuthoringComponent>() == null)
            {
                GhostAuthoringComponent ghost = root.AddComponent<GhostAuthoringComponent>();
                ghost.DefaultGhostMode = GhostMode.Interpolated;
                ghost.HasOwner = false;
                ghost.SupportAutoCommandTarget = false;
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, BulletPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
        }

        private static void BuildNfeScene(GameObject bulletVisual)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildEnvironment();

            GameObject subSceneObject = new("NFE Gameplay SubScene");
            SubScene subScene = subSceneObject.AddComponent<SubScene>();
            subScene.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(NfeSubScenePath);
            subScene.AutoLoadScene = true;

            GameObject bridge = new("NFE Presentation Bridge");
            GameObject playerVisual = AssetDatabase.LoadAssetAtPath<GameObject>(NgoPrefabPath);
            if (playerVisual == null)
            {
                Debug.LogError($"NFE player visual prefab not found at {NgoPrefabPath}.");
            }

            bridge.AddComponent<NfePresentationBridge>().Configure(playerVisual, bulletVisual);

            ConnectionPanel panel = BuildConnectionPanel("NFE — NETCODE FOR ENTITIES", "UDP 7980");
            NfeConnectionUI ui = panel.Canvas.gameObject.AddComponent<NfeConnectionUI>();
            ui.Configure(panel.Address, panel.Port, panel.Status, panel.LocalPlayer);
            panel.Port.text = NfeConnectionUI.DefaultPort.ToString();
            AddButton(panel.ButtonParent, "Start Server", new Vector2(0f, 0f), ui.StartServer);
            AddButton(panel.ButtonParent, "Start Host", new Vector2(220f, 0f), ui.StartHost);
            AddButton(panel.ButtonParent, "Connect Client", new Vector2(440f, 0f), ui.ConnectClient);

            EditorSceneManager.SaveScene(scene, NfeScenePath);
        }

        private static void BuildEnvironment()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2.4f, 1f, 1.8f);
            ground.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("NetworkGround", new Color(0.16f, 0.22f, 0.28f));

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10.5f;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 16f, -12f), Quaternion.Euler(52f, 0f, 0f));
        }

        private static ConnectionPanel BuildConnectionPanel(string titleValue, string protocol)
        {
            GameObject canvasObject = new("UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            Text title = CreateText(canvasObject.transform, "Title", titleValue, 32, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-600f, -65f), new Vector2(600f, -15f));
            Text protocolText = CreateText(canvasObject.transform, "Protocol", protocol, 20, TextAnchor.UpperCenter);
            SetRect(protocolText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-250f, -105f), new Vector2(250f, -70f));

            InputField address = CreateInput(canvasObject.transform, "ServerAddress", "127.0.0.1", new Vector2(35f, -45f));
            InputField port = CreateInput(canvasObject.transform, "ServerPort", "0", new Vector2(35f, -105f));
            Text status = CreateText(canvasObject.transform, "Status", "Status: Offline", 20, TextAnchor.UpperLeft);
            SetRect(status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(35f, -185f), new Vector2(600f, -145f));
            Text local = CreateText(canvasObject.transform, "LocalPlayer", "Local player: none", 20, TextAnchor.UpperLeft);
            SetRect(local.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(35f, -225f), new Vector2(600f, -185f));


            GameObject buttons = new("ConnectionButtons", typeof(RectTransform));
            buttons.transform.SetParent(canvasObject.transform, false);
            RectTransform buttonsRect = (RectTransform)buttons.transform;
            buttonsRect.anchorMin = new Vector2(0f, 1f);
            buttonsRect.anchorMax = new Vector2(0f, 1f);
            buttonsRect.anchoredPosition = new Vector2(35f, -280f);
            buttonsRect.sizeDelta = new Vector2(900f, 50f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return new ConnectionPanel(canvas, address, port, status, local, buttons.transform);
        }

        private static InputField CreateInput(Transform parent, string name, string value, Vector2 position)
        {
            GameObject inputObject = new(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)inputObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(360f, 46f);
            inputObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);

            Text text = CreateText(inputObject.transform, "Text", value, 20, TextAnchor.MiddleLeft);
            text.color = Color.black;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f));
            InputField field = inputObject.GetComponent<InputField>();
            field.textComponent = text;
            field.text = value;
            return field;
        }

        private static void AddButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new(label.Replace(" ", string.Empty), typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(155f, 44f);
            buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.75f);
            Text text = CreateText(buttonObject.transform, "Label", label, 18, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(buttonObject.GetComponent<Button>().onClick, action);
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureBuildSettings()
        {
            string[] paths = { "Assets/Scenes/LocalGameplayBaseline.unity", NgoScenePath, NfeScenePath };
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => !paths.Contains(scene.path))
                .ToList();
            scenes.InsertRange(0, paths.Select(path => new EditorBuildSettingsScene(path, true)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private readonly struct ConnectionPanel
        {
            public readonly Canvas Canvas;
            public readonly InputField Address;
            public readonly InputField Port;
            public readonly Text Status;
            public readonly Text LocalPlayer;
            public readonly Transform ButtonParent;

            public ConnectionPanel(Canvas canvas, InputField address, InputField port, Text status, Text localPlayer, Transform buttonParent)
            {
                Canvas = canvas;
                Address = address;
                Port = port;
                Status = status;
                LocalPlayer = localPlayer;
                ButtonParent = buttonParent;
            }
        }
    }
}
