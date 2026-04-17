using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Angled overhead camera. Follows the player when one is assigned,
    /// otherwise free-pans with WASD. Scroll wheel zooms.
    /// </summary>
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Settings")]
        public float zoomSpeed = 10f;
        public float minZoom = 3f;
        public float maxZoom = 18f;
        public float smoothSpeed = 8f;

        [Header("Angle")]
        public float pitchAngle = 55f;
        public float viewDistance = 14f;

        [Header("Follow")]
        public Transform followTarget;      // set to player transform

        private Vector3 _lookTarget;
        private float _targetZoom;
        private Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            _targetZoom = _cam != null ? _cam.orthographicSize : 6f;
        }

        void Start() => ApplyTransform(true);

        public void CenterOnGrid(GridManager grid)
        {
            _lookTarget = new Vector3(grid.GridWidth * 0.5f, 0f, grid.GridHeight * 0.5f);
            float fitZoom = Mathf.Max(grid.GridWidth, grid.GridHeight) * 0.55f;
            _targetZoom = Mathf.Clamp(fitZoom, minZoom, maxZoom);
            ApplyTransform(true);
        }

        void LateUpdate()
        {
            // Follow player if assigned; otherwise keep last look target
            if (followTarget != null)
                _lookTarget = new Vector3(followTarget.position.x, 0f, followTarget.position.z);

            HandleZoom();
            ApplyTransform(false);
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
        }

        private void ApplyTransform(bool instant)
        {
            float rad = pitchAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(0f, Mathf.Sin(rad), -Mathf.Cos(rad)) * viewDistance;
            Vector3 desiredPos = _lookTarget + offset;
            Quaternion desiredRot = Quaternion.Euler(pitchAngle, 0f, 0f);
            float t = Time.unscaledDeltaTime * smoothSpeed;

            transform.position = instant ? desiredPos : Vector3.Lerp(transform.position, desiredPos, t);
            transform.rotation = instant ? desiredRot : Quaternion.Slerp(transform.rotation, desiredRot, t);
            if (_cam != null)
                _cam.orthographicSize = instant ? _targetZoom
                    : Mathf.Lerp(_cam.orthographicSize, _targetZoom, t);
        }

        public bool ScreenToGroundPoint(Vector3 screenPos, out Vector3 worldPos)
        {
            if (_cam == null) { worldPos = Vector3.zero; return false; }
            Ray ray = _cam.ScreenPointToRay(screenPos);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                worldPos = ray.GetPoint(dist);
                return true;
            }
            worldPos = Vector3.zero;
            return false;
        }
    }
}
