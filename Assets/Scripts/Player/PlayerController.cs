using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Player character controller. Moves in XZ with WASD/arrows.
    /// Swap out the capsule mesh for your own model by assigning modelPrefab.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float rotateSpeed = 720f;    // degrees per second to face move direction

        [Header("Model")]
        public GameObject modelPrefab;      // assign your FBX prefab here; uses capsule if null

        private Rigidbody _rb;
        private GameObject _model;
        private Animator _animator;
        private static readonly int _speedHash = Animator.StringToHash("Speed");

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
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

        private void SpawnModel()
        {
            if (modelPrefab != null)
            {
                _model = Instantiate(modelPrefab, transform);
                _model.transform.localPosition = Vector3.zero;
            }
            else
            {
                // Placeholder capsule until a real model is assigned
                _model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _model.transform.SetParent(transform);
                _model.transform.localPosition = new Vector3(0f, 1f, 0f);
                _model.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                Destroy(_model.GetComponent<CapsuleCollider>());

                // Bright colour so the player stands out
                var mr = _model.GetComponent<MeshRenderer>();
                mr.material = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
                mr.material.color = new Color(0.2f, 0.6f, 1f);
            }

            _animator = _model.GetComponentInChildren<Animator>();
        }

        void FixedUpdate()
        {
            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    input.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  input.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;

            Vector3 move = input.sqrMagnitude > 0.01f
                ? input.normalized * moveSpeed
                : Vector3.zero;

            _rb.linearVelocity = new Vector3(move.x, _rb.linearVelocity.y, move.z);

            // Rotate model to face movement direction
            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                _model.transform.rotation = Quaternion.RotateTowards(
                    _model.transform.rotation, target,
                    rotateSpeed * Time.fixedDeltaTime);
            }

            if (_animator != null)
                _animator.SetFloat(_speedHash, move.magnitude);
        }
    }
}
