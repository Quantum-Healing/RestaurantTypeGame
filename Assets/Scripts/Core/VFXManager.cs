using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace KitchenEmpire
{
    /// <summary>
    /// Handles visual effects: floating text (+$20), particles on cooking completion,
    /// item pickup/place effects, customer satisfaction emojis.
    /// Gives the game that PlateUp! juicy feedback feel.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject floatingTextPrefab;
        public GameObject particleBurstPrefab;
        public GameObject cookCompleteVFXPrefab;

        [Header("Settings")]
        public float floatingTextSpeed = 1f;
        public float floatingTextDuration = 1.5f;

        private List<FloatingText> _floatingTexts = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnEnable()
        {
            GameEvents.OnRevenueEarned += OnRevenue;
            GameEvents.OnTipEarned += OnTip;
            GameEvents.OnMachineProcessingComplete += OnProcessingComplete;
            GameEvents.OnCustomerLeft += OnCustomerLeft;
        }

        void OnDisable()
        {
            GameEvents.OnRevenueEarned -= OnRevenue;
            GameEvents.OnTipEarned -= OnTip;
            GameEvents.OnMachineProcessingComplete -= OnProcessingComplete;
            GameEvents.OnCustomerLeft -= OnCustomerLeft;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.timer -= dt;
                ft.transform.position += Vector3.up * floatingTextSpeed * dt;

                if (ft.tmpText != null)
                {
                    float alpha = Mathf.Clamp01(ft.timer / floatingTextDuration);
                    var color = ft.tmpText.color;
                    color.a = alpha;
                    ft.tmpText.color = color;
                }

                if (ft.timer <= 0)
                {
                    Destroy(ft.gameObject);
                    _floatingTexts.RemoveAt(i);
                }
            }
        }

        public void SpawnFloatingText(Vector3 worldPos, string text, Color color)
        {
            GameObject go;
            if (floatingTextPrefab != null)
            {
                go = Instantiate(floatingTextPrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity, transform);
            }
            else
            {
                // Fallback: create a simple TextMeshPro
                go = new GameObject("FloatingText");
                go.transform.position = worldPos + Vector3.up * 0.5f;
                go.transform.SetParent(transform);
                var tmp = go.AddComponent<TextMeshPro>();
                tmp.text = text;
                tmp.color = color;
                tmp.fontSize = 4;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.sortingOrder = 100;
                // Face camera
                go.transform.rotation = Camera.main.transform.rotation;
            }

            var textComp = go.GetComponentInChildren<TextMeshPro>();
            if (textComp != null)
            {
                textComp.text = text;
                textComp.color = color;
            }

            _floatingTexts.Add(new FloatingText
            {
                gameObject = go,
                transform = go.transform,
                tmpText = textComp,
                timer = floatingTextDuration
            });
        }

        public void SpawnParticleBurst(Vector3 worldPos, Color color)
        {
            if (particleBurstPrefab != null)
            {
                var go = Instantiate(particleBurstPrefab, worldPos, Quaternion.identity);
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = color;
                }
                Destroy(go, 2f);
            }
        }

        private void OnRevenue(int amount)
        {
            // Find a serving counter to show the text at
            var gm = GameManager.Instance;
            if (gm == null) return;

            foreach (var kvp in gm.machineManager.AllMachines)
            {
                if (kvp.Value.definition.machineType == MachineType.ServingCounter)
                {
                    Vector3 pos = gm.gridManager.GridToWorld(kvp.Key);
                    SpawnFloatingText(pos, $"+${amount}", new Color(0.29f, 0.85f, 0.5f));
                    break;
                }
            }
        }

        private void OnTip(int amount)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            foreach (var kvp in gm.machineManager.AllMachines)
            {
                if (kvp.Value.definition.machineType == MachineType.ServingCounter)
                {
                    Vector3 pos = gm.gridManager.GridToWorld(kvp.Key) + Vector3.right * 0.3f;
                    SpawnFloatingText(pos, $"+${amount} tip!", new Color(0.984f, 0.749f, 0.149f));
                    break;
                }
            }
        }

        private void OnProcessingComplete(Vector2Int pos, IngredientType item)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            Vector3 worldPos = gm.gridManager.GridToWorld(pos);
            SpawnParticleBurst(worldPos + Vector3.up * 0.3f, new Color(0.984f, 0.749f, 0.149f));
            SpawnFloatingText(worldPos, "✓", Color.white);
        }

        private void OnCustomerLeft(int id, bool satisfied)
        {
            // Could spawn emoji particle - happy or angry face
        }

        private class FloatingText
        {
            public GameObject gameObject;
            public Transform transform;
            public TextMeshPro tmpText;
            public float timer;
        }
    }
}
