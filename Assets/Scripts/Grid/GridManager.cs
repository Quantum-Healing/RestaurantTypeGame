using UnityEngine;
using System.Collections.Generic;

namespace KitchenEmpire
{
    /// <summary>
    /// Manages the isometric grid: tile creation, coordinate conversion,
    /// and grid expansion.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject wallPrefab;

        [Header("Materials")]
        public Material floorMaterialA;
        public Material floorMaterialB;
        public Material wallMaterial;

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        private Dictionary<Vector2Int, GameObject> _tiles = new();
        private List<GameObject> _walls = new();

        public void InitializeGrid(int width, int height)
        {
            ClearGrid();
            GridWidth = width;
            GridHeight = height;
            BuildFloor();
            BuildWalls();
        }

        public void ExpandGrid(int newWidth, int newHeight)
        {
            if (newWidth <= GridWidth && newHeight <= GridHeight) return;
            InitializeGrid(
                Mathf.Max(GridWidth, newWidth),
                Mathf.Max(GridHeight, newHeight)
            );
            GameEvents.FireKitchenExpanded(GridWidth, GridHeight);
        }

        private void BuildFloor()
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    Vector3 worldPos = GridToWorld(x, y);
                    GameObject tile;
                    if (tilePrefab != null)
                    {
                        tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        // Fallback: create a flat quad
                        tile = CreateIsometricTile(worldPos);
                    }

                    tile.name = $"Tile_{x}_{y}";

                    // Checkerboard
                    var renderer = tile.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        bool isAlt = (x + y) % 2 == 0;
                        if (isAlt && floorMaterialA != null)
                            renderer.material = floorMaterialA;
                        else if (!isAlt && floorMaterialB != null)
                            renderer.material = floorMaterialB;
                    }

                    _tiles[new Vector2Int(x, y)] = tile;
                }
            }
        }

        private void BuildWalls()
        {
            // Camera looks from the southeast (Euler 35, -45, 0), so back walls
            // are at the far edges: y = GridHeight-1 and x = GridWidth-1.

            // Back wall along y = GridHeight-1 (runs in +X world direction)
            for (int x = 0; x < GridWidth; x++)
            {
                Vector3 pos = GridToWorld(x, GridHeight - 1) + new Vector3(0, 0.3f, 0.25f);
                GameObject wall = CreateWallSegment(pos, true);
                _walls.Add(wall);
            }

            // Back wall along x = GridWidth-1 (runs in -X world direction as y increases)
            for (int y = 0; y < GridHeight; y++)
            {
                Vector3 pos = GridToWorld(GridWidth - 1, y) + new Vector3(0.25f, 0.3f, 0);
                GameObject wall = CreateWallSegment(pos, false);
                _walls.Add(wall);
            }
        }

        private GameObject CreateIsometricTile(Vector3 position)
        {
            // Create a flat diamond-shaped quad for the floor
            GameObject tile = new GameObject("FloorTile");
            tile.transform.position = position;

            MeshFilter mf = tile.AddComponent<MeshFilter>();
            MeshRenderer mr = tile.AddComponent<MeshRenderer>();

            // Diamond mesh for isometric tile
            Mesh mesh = new Mesh();
            float hw = 0.5f;
            float hh = 0.25f;

            mesh.vertices = new Vector3[]
            {
                new Vector3(0, 0, hh),      // top
                new Vector3(hw, 0, 0),       // right
                new Vector3(0, 0, -hh),      // bottom
                new Vector3(-hw, 0, 0)       // left
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new Vector2[] {
                new Vector2(0.5f, 1), new Vector2(1, 0.5f),
                new Vector2(0.5f, 0), new Vector2(0, 0.5f)
            };

            mf.mesh = mesh;

            // Add collider for raycasting
            MeshCollider mc = tile.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            return tile;
        }

        private GameObject CreateWallSegment(Vector3 position, bool isBackLeft)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.parent = transform;
            wall.transform.position = position;
            wall.transform.localScale = isBackLeft
                ? new Vector3(0.5f, 0.6f, 0.05f)
                : new Vector3(0.05f, 0.6f, 0.5f);
            wall.name = "Wall";

            if (wallMaterial != null)
            {
                wall.GetComponent<Renderer>().material = wallMaterial;
            }

            return wall;
        }

        private void ClearGrid()
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile != null) Destroy(tile);
            }
            _tiles.Clear();

            foreach (var wall in _walls)
            {
                if (wall != null) Destroy(wall);
            }
            _walls.Clear();
        }

        // ===== COORDINATE CONVERSION =====

        /// <summary>
        /// Convert grid coordinates to world position (isometric).
        /// Uses a standard isometric projection where:
        /// - X-axis goes to the bottom-right
        /// - Y-axis goes to the bottom-left
        /// </summary>
        public Vector3 GridToWorld(int gx, int gy)
        {
            float worldX = (gx - gy) * 0.5f;
            float worldZ = (gx + gy) * 0.25f;
            return new Vector3(worldX, 0f, worldZ);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return GridToWorld(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Convert world position back to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float gx = worldPos.x + worldPos.z * 2f;
            float gy = worldPos.z * 2f - worldPos.x;
            return new Vector2Int(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
        }

        public bool IsValidGridPos(Vector2Int pos)
        {
            return pos.x >= 0 && pos.y >= 0 && pos.x < GridWidth && pos.y < GridHeight;
        }

        public bool IsValidGridPos(int x, int y)
        {
            return x >= 0 && y >= 0 && x < GridWidth && y < GridHeight;
        }

        // ===== TILE HIGHLIGHTING =====

        public void HighlightTile(Vector2Int pos, Color color)
        {
            if (_tiles.TryGetValue(pos, out var tile))
            {
                var renderer = tile.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        public void ClearHighlight(Vector2Int pos)
        {
            if (_tiles.TryGetValue(pos, out var tile))
            {
                var renderer = tile.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    bool isAlt = (pos.x + pos.y) % 2 == 0;
                    renderer.material = isAlt ? floorMaterialA : floorMaterialB;
                }
            }
        }
    }
}
