using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace SkierFramework.Editor
{
    /// <summary>
    /// 编辑器工具：自动生成地牢 Tilemap
    /// 使用 BSP（二叉空间分割）算法生成房间和走廊
    /// 菜单：Tools → Dungeon Generator
    /// </summary>
    public class DungeonGeneratorWindow : EditorWindow
    {
        [Header("地图尺寸")]
        private int mapWidth = 60;
        private int mapHeight = 60;

        [Header("房间参数")]
        private int minRoomSize = 6;
        private int maxRoomSize = 14;
        private int maxDepth = 5;
        private int corridorWidth = 2;

        [Header("瓦片资源路径")]
        private string tilePath = "Assets/ArtRes/kenney_scribble-dungeons/";

        private int seed = 0;
        private bool useRandomSeed = true;

        // 瓦片引用
        private Sprite floorSprite;
        private Sprite wallSprite;
        private Sprite wallCornerSprite;
        private Sprite wallEdgeSprite;
        private Sprite doorOpenSprite;
        private Sprite grassSprite;

        // 生成的地图数据
        private int[,] map; // 0=void, 1=floor, 2=wall

        [MenuItem("Tools/Dungeon Generator")]
        public static void ShowWindow()
        {
            GetWindow<DungeonGeneratorWindow>("Dungeon Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("地牢生成器", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("地图参数", EditorStyles.boldLabel);
            mapWidth = EditorGUILayout.IntSlider("宽度", mapWidth, 30, 120);
            mapHeight = EditorGUILayout.IntSlider("高度", mapHeight, 30, 120);

            GUILayout.Space(5);
            GUILayout.Label("房间参数", EditorStyles.boldLabel);
            minRoomSize = EditorGUILayout.IntSlider("最小房间", minRoomSize, 4, 10);
            maxRoomSize = EditorGUILayout.IntSlider("最大房间", maxRoomSize, 8, 20);
            maxDepth = EditorGUILayout.IntSlider("分割深度", maxDepth, 3, 8);
            corridorWidth = EditorGUILayout.IntSlider("走廊宽度", corridorWidth, 1, 3);

            GUILayout.Space(5);
            useRandomSeed = EditorGUILayout.Toggle("随机种子", useRandomSeed);
            if (!useRandomSeed)
            {
                seed = EditorGUILayout.IntField("种子", seed);
            }

            GUILayout.Space(5);
            tilePath = EditorGUILayout.TextField("瓦片目录", tilePath);

            GUILayout.Space(15);

            if (GUILayout.Button("生成地牢", GUILayout.Height(40)))
            {
                GenerateDungeon();
            }

            if (GUILayout.Button("清除地牢", GUILayout.Height(25)))
            {
                ClearDungeon();
            }
        }

        private void GenerateDungeon()
        {
            if (!LoadTileSprites())
            {
                Debug.LogError("[DungeonGenerator] 加载瓦片失败，请检查路径");
                return;
            }

            if (useRandomSeed)
                seed = System.Environment.TickCount;

            Random.InitState(seed);
            Debug.Log($"[DungeonGenerator] Seed: {seed}");

            // 1. 初始化地图
            map = new int[mapWidth, mapHeight];

            // 2. BSP 生成房间
            var rooms = new List<RectInt>();
            var root = new BSPNode(new RectInt(1, 1, mapWidth - 2, mapHeight - 2));
            SplitNode(root, 0, rooms);

            // 3. 填充房间为地板
            foreach (var room in rooms)
            {
                for (int x = room.x; x < room.x + room.width; x++)
                {
                    for (int y = room.y; y < room.y + room.height; y++)
                    {
                        map[x, y] = 1;
                    }
                }
            }

            // 4. 连接房间（走廊）
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                ConnectRooms(rooms[i], rooms[i + 1]);
            }

            // 5. 生成墙壁
            GenerateWalls();

            // 6. 创建 Tilemap
            CreateTilemap(rooms);

            Debug.Log($"[DungeonGenerator] 生成完毕，{rooms.Count} 个房间");
        }

        #region BSP Algorithm

        private class BSPNode
        {
            public RectInt area;
            public BSPNode left;
            public BSPNode right;
            public RectInt? room;

            public BSPNode(RectInt area) { this.area = area; }
        }

        private void SplitNode(BSPNode node, int depth, List<RectInt> rooms)
        {
            if (depth >= maxDepth || node.area.width < minRoomSize * 2 || node.area.height < minRoomSize * 2)
            {
                // 叶节点：创建房间
                int roomW = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.area.width - 1));
                int roomH = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.area.height - 1));
                int roomX = Random.Range(node.area.x, node.area.x + node.area.width - roomW);
                int roomY = Random.Range(node.area.y, node.area.y + node.area.height - roomH);

                node.room = new RectInt(roomX, roomY, roomW, roomH);
                rooms.Add(node.room.Value);
                return;
            }

            bool splitH = Random.value > 0.5f;
            if (node.area.width > node.area.height * 1.3f) splitH = false;
            if (node.area.height > node.area.width * 1.3f) splitH = true;

            if (splitH)
            {
                int splitY = Random.Range(node.area.y + minRoomSize, node.area.y + node.area.height - minRoomSize);
                node.left = new BSPNode(new RectInt(node.area.x, node.area.y, node.area.width, splitY - node.area.y));
                node.right = new BSPNode(new RectInt(node.area.x, splitY, node.area.width, node.area.y + node.area.height - splitY));
            }
            else
            {
                int splitX = Random.Range(node.area.x + minRoomSize, node.area.x + node.area.width - minRoomSize);
                node.left = new BSPNode(new RectInt(node.area.x, node.area.y, splitX - node.area.x, node.area.height));
                node.right = new BSPNode(new RectInt(splitX, node.area.y, node.area.x + node.area.width - splitX, node.area.height));
            }

            SplitNode(node.left, depth + 1, rooms);
            SplitNode(node.right, depth + 1, rooms);
        }

        #endregion

        #region Corridors & Walls

        private void ConnectRooms(RectInt a, RectInt b)
        {
            Vector2Int centerA = new Vector2Int(a.x + a.width / 2, a.y + a.height / 2);
            Vector2Int centerB = new Vector2Int(b.x + b.width / 2, b.y + b.height / 2);

            // L 形走廊
            if (Random.value > 0.5f)
            {
                CarveCorridorH(centerA.x, centerB.x, centerA.y);
                CarveCorridorV(centerA.y, centerB.y, centerB.x);
            }
            else
            {
                CarveCorridorV(centerA.y, centerB.y, centerA.x);
                CarveCorridorH(centerA.x, centerB.x, centerB.y);
            }
        }

        private void CarveCorridorH(int x1, int x2, int y)
        {
            int minX = Mathf.Min(x1, x2);
            int maxX = Mathf.Max(x1, x2);
            for (int x = minX; x <= maxX; x++)
            {
                for (int w = 0; w < corridorWidth; w++)
                {
                    int cy = y + w;
                    if (cy >= 0 && cy < mapHeight && x >= 0 && x < mapWidth)
                        map[x, cy] = 1;
                }
            }
        }

        private void CarveCorridorV(int y1, int y2, int x)
        {
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);
            for (int y = minY; y <= maxY; y++)
            {
                for (int w = 0; w < corridorWidth; w++)
                {
                    int cx = x + w;
                    if (cx >= 0 && cx < mapWidth && y >= 0 && y < mapHeight)
                        map[cx, y] = 1;
                }
            }
        }

        private void GenerateWalls()
        {
            int[,] copy = (int[,])map.Clone();
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (copy[x, y] != 0) continue;
                    // 如果相邻有地板，则为墙
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight && copy[nx, ny] == 1)
                            {
                                map[x, y] = 2;
                                goto nextTile;
                            }
                        }
                    }
                    nextTile:;
                }
            }
        }

        #endregion

        #region Tilemap Creation

        private bool LoadTileSprites()
        {
            floorSprite = LoadSprite("tile.png");
            wallSprite = LoadSprite("wall.png");
            wallCornerSprite = LoadSprite("wall_corner.png");
            wallEdgeSprite = LoadSprite("wall_edge.png");
            doorOpenSprite = LoadSprite("door_open.png");
            grassSprite = LoadSprite("grass.png");

            return floorSprite != null && wallSprite != null;
        }

        private Sprite LoadSprite(string fileName)
        {
            string path = tilePath + fileName;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[DungeonGenerator] Sprite not found: {path}");
            }
            return sprite;
        }

        private void CreateTilemap(List<RectInt> rooms)
        {
            // 清除旧的
            ClearDungeon();

            // 创建 Grid
            var gridObj = new GameObject("DungeonGrid");
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);
            Undo.RegisterCreatedObjectUndo(gridObj, "Create Dungeon");

            // Floor 层
            var floorObj = CreateTilemapLayer(gridObj.transform, "Floor", 0);
            var floorTilemap = floorObj.GetComponent<Tilemap>();

            // Wall 层
            var wallObj = CreateTilemapLayer(gridObj.transform, "Walls", 1);
            var wallTilemap = wallObj.GetComponent<Tilemap>();
            var wallCollider = wallObj.AddComponent<TilemapCollider2D>();
            wallObj.AddComponent<CompositeCollider2D>();
            wallCollider.usedByComposite = true;
            var wallRb = wallObj.GetComponent<Rigidbody2D>();
            if (wallRb != null) wallRb.bodyType = RigidbodyType2D.Static;

            // 创建 Tile 资产
            Tile floorTile = CreateTile("FloorTile", floorSprite);
            Tile wallTile = CreateTile("WallTile", wallSprite);
            Tile wallCornerTile = wallCornerSprite != null ? CreateTile("WallCornerTile", wallCornerSprite) : wallTile;
            Tile grassTile = grassSprite != null ? CreateTile("GrassTile", grassSprite) : null;

            // 额外的地板变体（增加视觉多样性）
            var floorVariants = new List<Sprite>();
            floorVariants.Add(floorSprite);
            var planks = LoadSprite("planks.png");
            if (planks != null) floorVariants.Add(planks);
            var tilesCenter = LoadSprite("tiles_center.png");
            if (tilesCenter != null) floorVariants.Add(tilesCenter);
            var tilesDecorative = LoadSprite("tiles_decorative.png");
            if (tilesDecorative != null) floorVariants.Add(tilesDecorative);

            // 填充瓦片
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    switch (map[x, y])
                    {
                        case 1: // Floor
                            Sprite chosen = floorVariants[Random.Range(0, floorVariants.Count)];
                            Tile ft = CreateTile("ft_" + x + "_" + y, chosen);
                            floorTilemap.SetTile(pos, ft);
                            break;
                        case 2: // Wall
                            wallTilemap.SetTile(pos, IsCorner(x, y) ? wallCornerTile : wallTile);
                            break;
                        default: // Void — 可选填草地
                            if (grassTile != null)
                                floorTilemap.SetTile(pos, grassTile);
                            break;
                    }
                }
            }

            // 放置出生点标记
            if (rooms.Count > 0)
            {
                var firstRoom = rooms[0];
                var spawnObj = new GameObject("SpawnPoint");
                spawnObj.transform.parent = gridObj.transform;
                spawnObj.transform.position = new Vector3(
                    firstRoom.x + firstRoom.width / 2f,
                    firstRoom.y + firstRoom.height / 2f,
                    0);
                Undo.RegisterCreatedObjectUndo(spawnObj, "Create SpawnPoint");
            }

            Selection.activeGameObject = gridObj;
        }

        private GameObject CreateTilemapLayer(Transform parent, string name, int sortOrder)
        {
            var obj = new GameObject(name);
            obj.transform.parent = parent;
            var tm = obj.AddComponent<Tilemap>();
            var tr = obj.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortOrder;
            return obj;
        }

        private Tile CreateTile(string name, Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.name = name;
            return tile;
        }

        private bool IsCorner(int x, int y)
        {
            // 简化判定：如果两个正交方向都是墙，算角落
            int wallCount = 0;
            if (GetTile(x - 1, y) == 2) wallCount++;
            if (GetTile(x + 1, y) == 2) wallCount++;
            if (GetTile(x, y - 1) == 2) wallCount++;
            if (GetTile(x, y + 1) == 2) wallCount++;
            return wallCount >= 2 && wallCount < 4;
        }

        private int GetTile(int x, int y)
        {
            if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return 0;
            return map[x, y];
        }

        #endregion

        private void ClearDungeon()
        {
            var existing = GameObject.Find("DungeonGrid");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }
    }
}
