using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Player character: WASD movement + E-key machine interaction.
    /// Swap the capsule for a real model by assigning modelPrefab.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float rotateSpeed = 720f;

        [Header("Interaction")]
        public float interactRadius = 1.4f;

        [Header("Model")]
        public GameObject modelPrefab;

        [HideInInspector] public InputHandler inputHandler;
        [HideInInspector] public GridManager gridManager;
        [HideInInspector] public UIManager uiManager;

        private Rigidbody _rb;
        private GameObject _model;
        private Animator _animator;
        private static readonly int _speedHash = Animator.StringToHash("Speed");

        void Awake()
        {
            _rb = gameObject.GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
                _rb.freezeRotation = true;
                _rb.constraints = RigidbodyConstraints.FreezePositionY
                                | RigidbodyConstraints.FreezeRotationX
                                | RigidbodyConstraints.FreezeRotationZ;
            }

            SpawnModel();
        }

        void Update()
        {
            bool nearMachine = HasNearbyMachine();
            if (uiManager != null && uiManager.interactHint != null)
                uiManager.interactHint.gameObject.SetActive(nearMachine);

            if (Input.GetKeyDown(KeyCode.E))
                TryInteract();
        }

        private bool HasNearbyMachine()
        {
            if (gridManager == null || inputHandler == null) return false;
            var mm = inputHandler.machineManager;
            if (mm == null) return false;

            Vector3 pos = transform.position;
            int range = Mathf.CeilToInt(interactRadius) + 1;
            Vector2Int center = gridManager.WorldToGrid(pos);

            for (int dx = -range; dx <= range; dx++)
                for (int dz = -range; dz <= range; dz++)
                {
                    var cell = new Vector2Int(center.x + dx, center.y + dz);
                    if (!gridManager.IsValidGridPos(cell)) continue;
                    Vector3 cellWorld = gridManager.GridToWorld(cell.x, cell.y);
                    if (Vector3.Distance(pos, cellWorld) <= interactRadius
                        && mm.GetMachine(cell) != null)
                        return true;
                }
            return false;
        }

        void FixedUpdate()
        {
            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    input.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  input.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;

            Vector3 move = input.sqrMagnitude > 0.01f ? input.normalized * moveSpeed : Vector3.zero;
            _rb.velocity = new Vector3(move.x, _rb.velocity.y, move.z);

            if (move.sqrMagnitude > 0.01f && _model != null)
            {
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                _model.transform.rotation = Quaternion.RotateTowards(
                    _model.transform.rotation, target, rotateSpeed * Time.fixedDeltaTime);
            }

            if (_animator != null)
                _animator.SetFloat(_speedHash, move.magnitude);
        }

        private void TryInteract()
        {
            if (inputHandler == null || gridManager == null) return;

            // Find nearest grid cell within interact radius that has a machine
            Vector2Int? nearest = null;
            float bestDist = float.MaxValue;

            Vector3 pos = transform.position;
            int range = Mathf.CeilToInt(interactRadius) + 1;
            Vector2Int center = gridManager.WorldToGrid(pos);

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dz = -range; dz <= range; dz++)
                {
                    var cell = new Vector2Int(center.x + dx, center.y + dz);
                    if (!gridManager.IsValidGridPos(cell)) continue;

                    Vector3 cellWorld = gridManager.GridToWorld(cell.x, cell.y);
                    float dist = Vector3.Distance(pos, cellWorld);
                    if (dist <= interactRadius && dist < bestDist)
                    {
                        bestDist = dist;
                        nearest = cell;
                    }
                }
            }

            if (nearest.HasValue)
                inputHandler.InteractAt(nearest.Value);
        }

        private void SpawnModel()
        {
            if (modelPrefab != null)
            {
                _model = Instantiate(modelPrefab, transform);
                _model.transform.localPosition = Vector3.zero;
            }
            else
            {
                _model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _model.transform.SetParent(transform);
                _model.transform.localPosition = new Vector3(0f, 1f, 0f);
                _model.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                Destroy(_model.GetComponent<CapsuleCollider>());

                var mr = _model.GetComponent<MeshRenderer>();
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mr.material = new Material(shader);
                mr.material.color = new Color(0.2f, 0.6f, 1f);
            }

            _animator = _model.GetComponentInChildren<Animator>();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
