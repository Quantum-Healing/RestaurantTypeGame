using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// 2.5D camera controller. Orthographic, fixed at Z=50 facing -Z.
    /// Pans on X/Y axes only. Scroll wheel zooms orthographic size.
    /// </summary>
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Settings")]
        public float panSpeed = 10f;
        public float zoomSpeed = 2f;
        public float minZoom = 2f;
        public float maxZoom = 15f;
        public float smoothSpeed = 8f;

        private Vector2 _targetPosition;
        private float _targetZoom;
        private Camera _cam;

        private const float CameraZ = 50f;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            _targetZoom = _cam != null ? _cam.orthographicSize : 5f;
            _targetPosition = new Vector2(transform.position.x, transform.position.y);
        }

        void Start()
        {
            transform.SetPositionAndRotation(
                new Vector3(_targetPosition.x, _targetPosition.y, CameraZ),
                Quaternion.Euler(0f, 180f, 0f));
        }

        public void CenterOnGrid(GridManager grid)
        {
            Vector3 center = grid.GridToWorld(grid.GridWidth / 2, grid.GridHeight / 2);
            _targetPosition = new Vector2(center.x, center.y);
            ApplyPosition(true);
        }

        void Update()
        {
            HandlePanning();
            HandleZoom();
            ApplyPosition(false);
        }

        private void HandlePanning()
        {
            Vector2 input = Vector2.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    input.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  input.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  input.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1;

            if (input.sqrMagnitude > 0.01f)
                _targetPosition += input.normalized * panSpeed * Time.unscaledDeltaTime;
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

        private void ApplyPosition(bool instant)
        {
            Vector3 desiredPos = new Vector3(_targetPosition.x, _targetPosition.y, CameraZ);
            float desiredZoom = _targetZoom;

            if (instant)
            {
                transform.position = desiredPos;
                if (_cam != null) _cam.orthographicSize = desiredZoom;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPos,
                    Time.unscaledDeltaTime * smoothSpeed);
                if (_cam != null)
                    _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, desiredZoom,
                        Time.unscaledDeltaTime * smoothSpeed);
            }
        }

        /// <summary>
        /// Project screen point onto the Z=0 plane.
        /// </summary>
        public bool ScreenToGroundPoint(Vector3 screenPos, out Vector3 worldPos)
        {
            if (_cam == null) { worldPos = Vector3.zero; return false; }

            Ray ray = _cam.ScreenPointToRay(screenPos);
            Plane frontPlane = new Plane(Vector3.forward, Vector3.zero);

            if (frontPlane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                return true;
            }
            worldPos = Vector3.zero;
            return false;
        }
    }
}
