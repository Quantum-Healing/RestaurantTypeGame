using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Angled overhead camera (Stardew Valley style).
    /// Pitches down ~55 degrees looking along +Z. No yaw — grid stays straight.
    /// Pan with WASD, zoom with scroll wheel.
    /// </summary>
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Settings")]
        public float panSpeed = 8f;
        public float zoomSpeed = 10f;
        public float minZoom = 3f;
        public float maxZoom = 18f;
        public float smoothSpeed = 10f;

        [Header("Angle")]
        public float pitchAngle = 55f;
        public float viewDistance = 14f;

        private Vector3 _lookTarget;
        private float _targetZoom;
        private Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            _targetZoom = _cam != null ? _cam.orthographicSize : 6f;
        }

        void Start()
        {
            ApplyTransform(true);
        }

        public void CenterOnGrid(GridManager grid)
        {
            _lookTarget = new Vector3(grid.GridWidth * 0.5f, 0f, grid.GridHeight * 0.5f);
            float fitZoom = Mathf.Max(grid.GridWidth, grid.GridHeight) * 0.55f;
            _targetZoom = Mathf.Clamp(fitZoom, minZoom, maxZoom);
            ApplyTransform(true);
        }

        void Update()
        {
            HandlePanning();
            HandleZoom();
            ApplyTransform(false);
        }

        private void HandlePanning()
        {
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    move.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  move.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  move.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;

            if (move.sqrMagnitude > 0.01f)
                _lookTarget += move.normalized * panSpeed * Time.unscaledDeltaTime;
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
                _cam.orthographicSize = instant ? _targetZoom : Mathf.Lerp(_cam.orthographicSize, _targetZoom, t);
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
