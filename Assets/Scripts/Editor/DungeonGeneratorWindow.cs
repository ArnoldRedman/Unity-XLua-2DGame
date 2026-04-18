using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace SkierFramework.Tools
{
    /// <summary>
    /// 编辑器工具：自动生成地牢 Tilemap
    /// BSP 算法 + 墙壁朝向自动判定
    /// 菜单：Tools → Dungeon Generator
    /// </summary>
    public class DungeonGeneratorWindow : EditorWindow
    {
        private int mapWidth = 60;
        private int mapHeight = 60;
        private int minRoomSize = 6;
        private int maxRoomSize = 14;
        private int maxDepth = 5;
        private int corridorWidth = 2;
        private string tilePath = "Assets/ArtRes/kenney_scribble-dungeons/";
        private int seed = 0;
        private bool useRandomSeed = true;

        private int[,] map; // 0=void, 1=floor, 2=wall

        [MenuItem("Tools/Dungeon Generator")]
        public static void ShowWindow()
        {
            GetWindow<DungeonGeneratorWindow>("Dungeon Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("地牢生成器 (Kenney Scribble Dungeons)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            mapWidth = EditorGUILayout.IntSlider("宽度", mapWidth, 30, 120);
            mapHeight = EditorGUILayout.IntSlider("高度", mapHeight, 30, 120);
            GUILayout.Space(5);
            minRoomSize = EditorGUILayout.IntSlider("最小房间", minRoomSize, 4, 10);
            maxRoomSize = EditorGUILayout.IntSlider("最大房间", maxRoomSize, 8, 20);
            maxDepth = EditorGUILayout.IntSlider("分割深度", maxDepth, 3, 8);
            corridorWidth = EditorGUILayout.IntSlider("走廊宽度", corridorWidth, 1, 3);
            GUILayout.Space(5);
            useRandomSeed = EditorGUILayout.Toggle("随机种子", useRandomSeed);
            if (!useRandomSeed) seed = EditorGUILayout.IntField("种子", seed);
            GUILayout.Space(5);
            tilePath = EditorGUILayout.TextField("瓦片目录", tilePath);
            GUILayout.Space(15);

            if (GUILayout.Button("生成地牢", GUILayout.Height(40)))
                GenerateDungeon();
            if (GUILayout.Button("清除地牢", GUILayout.Height(25)))
                ClearDungeon();
        }

        private void GenerateDungeon()
        {
            if (useRandomSeed) seed = System.Environment.TickCount;
            Random.InitState(seed);
            Debug.Log($"[DungeonGenerator] Seed: {seed}");

            map = new int[mapWidth, mapHeight];
            var rooms = new List<RectInt>();
            SplitNode(new BSPNode(new RectInt(1, 1, mapWidth - 2, mapHeight - 2)), 0, rooms);

            foreach (var room in rooms)
                for (int x = room.x; x < room.x + room.width; x++)
                    for (int y = room.y; y < room.y + room.height; y++)
                        map[x, y] = 1;

            for (int i = 0; i < rooms.Count - 1; i++)
                ConnectRooms(rooms[i], rooms[i + 1]);

            GenerateWalls();
            BuildTilemap(rooms);
            Debug.Log($"[DungeonGenerator] 生成完毕，{rooms.Count} 个房间");
        }

        #region BSP

        private class BSPNode
        {
            public RectInt area;
            public BSPNode left, right;
            public BSPNode(RectInt area) { this.area = area; }
        }

        private void SplitNode(BSPNode node, int depth, List<RectInt> rooms)
        {
            if (depth >= maxDepth || node.area.width < minRoomSize * 2 || node.area.height < minRoomSize * 2)
            {
                int rw = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.area.width - 1));
                int rh = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.area.height - 1));
                int rx = Random.Range(node.area.x, node.area.x + node.area.width - rw);
                int ry = Random.Range(node.area.y, node.area.y + node.area.height - rh);
                rooms.Add(new RectInt(rx, ry, rw, rh));
                return;
            }

            bool splitH = Random.value > 0.5f;
            if (node.area.width > node.area.height * 1.3f) splitH = false;
            if (node.area.height > node.area.width * 1.3f) splitH = true;

            if (splitH)
            {
                int sy = Random.Range(node.area.y + minRoomSize, node.area.y + node.area.height - minRoomSize);
                node.left = new BSPNode(new RectInt(node.area.x, node.area.y, node.area.width, sy - node.area.y));
                node.right = new BSPNode(new RectInt(node.area.x, sy, node.area.width, node.area.y + node.area.height - sy));
            }
            else
            {
                int sx = Random.Range(node.area.x + minRoomSize, node.area.x + node.area.width - minRoomSize);
                node.left = new BSPNode(new RectInt(node.area.x, node.area.y, sx - node.area.x, node.area.height));
                node.right = new BSPNode(new RectInt(sx, node.area.y, node.area.x + node.area.width - sx, node.area.height));
            }

            SplitNode(node.left, depth + 1, rooms);
            SplitNode(node.right, depth + 1, rooms);
        }

        #endregion

        #region Corridors & Walls

        private void ConnectRooms(RectInt a, RectInt b)
        {
            var ca = new Vector2Int(a.x + a.width / 2, a.y + a.height / 2);
            var cb = new Vector2Int(b.x + b.width / 2, b.y + b.height / 2);
            if (Random.value > 0.5f) { CarveH(ca.x, cb.x, ca.y); CarveV(ca.y, cb.y, cb.x); }
            else { CarveV(ca.y, cb.y, ca.x); CarveH(ca.x, cb.x, cb.y); }
        }

        private void CarveH(int x1, int x2, int y)
        {
            for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
                for (int w = 0; w < corridorWidth; w++)
                    SetFloor(x, y + w);
        }

        private void CarveV(int y1, int y2, int x)
        {
            for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                for (int w = 0; w < corridorWidth; w++)
                    SetFloor(x + w, y);
        }

        private void SetFloor(int x, int y)
        {
            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                map[x, y] = 1;
        }

        private void GenerateWalls()
        {
            int[,] copy = (int[,])map.Clone();
            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    if (copy[x, y] != 0) continue;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight && copy[nx, ny] == 1)
                            { map[x, y] = 2; goto done; }
                        }
                    done:;
                }
        }

        #endregion

        #region Wall Classification

        private int Get(int x, int y) => (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight) ? map[x, y] : 0;
        private bool F(int x, int y) => Get(x, y) == 1; // is floor

        /// <summary>
        /// 根据周围地板分布，决定使用哪个墙壁 sprite 和旋转角度
        /// </summary>
        private void GetWallSpriteAndRotation(int x, int y,
            Sprite sprWall, Sprite sprCorner, Sprite sprCurve, Sprite sprEdge,
            out Sprite sprite, out float rotation)
        {
            bool N = F(x, y + 1), S = F(x, y - 1), E = F(x + 1, y), W = F(x - 1, y);
            bool NE = F(x + 1, y + 1), NW = F(x - 1, y + 1), SE = F(x + 1, y - 1), SW = F(x - 1, y - 1);

            int orth = (N ? 1 : 0) + (S ? 1 : 0) + (E ? 1 : 0) + (W ? 1 : 0);

            // 直墙：只有一个正交方向有地板
            if (orth == 1)
            {
                sprite = sprWall;
                // wall.png 默认朝向是地板在南方（下方）
                if (S) { rotation = 0; return; }
                if (W) { rotation = 270; return; }
                if (N) { rotation = 180; return; }
                if (E) { rotation = 90; return; }
            }

            // 外角：两个相邻正交方向有地板
            if (orth == 2 && sprCorner != null)
            {
                // 相邻的两个方向
                if (S && E) { sprite = sprCorner; rotation = 0; return; }
                if (S && W) { sprite = sprCorner; rotation = 270; return; }
                if (N && W) { sprite = sprCorner; rotation = 180; return; }
                if (N && E) { sprite = sprCorner; rotation = 90; return; }
                // 对面的两个方向（走廊中间的墙）→ 用直墙
                if (N && S) { sprite = sprWall; rotation = 0; return; }
                if (E && W) { sprite = sprWall; rotation = 90; return; }
            }

            // 内角：没有正交方向地板，只有对角方向
            if (orth == 0 && sprCurve != null)
            {
                if (SE) { sprite = sprCurve; rotation = 0; return; }
                if (SW) { sprite = sprCurve; rotation = 270; return; }
                if (NW) { sprite = sprCurve; rotation = 180; return; }
                if (NE) { sprite = sprCurve; rotation = 90; return; }
            }

            // 三面地板或更多 → 用 edge
            if (orth >= 3 && sprEdge != null)
            {
                sprite = sprEdge; rotation = 0; return;
            }

            // 默认
            sprite = sprWall;
            rotation = 0;
        }

        #endregion

        #region Tilemap Build

        private void BuildTilemap(List<RectInt> rooms)
        {
            ClearDungeon();

            // 加载素材
            Sprite sprFloor = LoadSprite("tile.png");
            Sprite sprFloorCracked = LoadSprite("tiles_cracked.png");
            Sprite sprFloorDeco = LoadSprite("tiles_decorative.png");
            Sprite sprFloorCenter = LoadSprite("tiles_center.png");
            Sprite sprWall = LoadSprite("wall.png");
            Sprite sprCorner = LoadSprite("wall_corner.png");
            Sprite sprCurve = LoadSprite("wall_curve.png");
            Sprite sprEdge = LoadSprite("wall_edge.png");
            Sprite sprGrass = LoadSprite("grass.png");

            if (sprFloor == null || sprWall == null)
            {
                Debug.LogError("[DungeonGenerator] 缺少 tile.png 或 wall.png");
                return;
            }

            // 地板变体（加权：普通地板 70%，变体 30%）
            var floorSprites = new List<Sprite>();
            for (int i = 0; i < 7; i++) floorSprites.Add(sprFloor); // 70%
            if (sprFloorCracked != null) floorSprites.Add(sprFloorCracked);
            if (sprFloorDeco != null) floorSprites.Add(sprFloorDeco);
            if (sprFloorCenter != null) floorSprites.Add(sprFloorCenter);

            // Grid
            var gridObj = new GameObject("DungeonGrid");
            gridObj.AddComponent<Grid>().cellSize = new Vector3(1, 1, 0);
            Undo.RegisterCreatedObjectUndo(gridObj, "Create Dungeon");

            // Floor Tilemap
            var floorGO = MakeTilemapLayer(gridObj.transform, "Floor", 0);
            var floorTM = floorGO.GetComponent<Tilemap>();

            // Wall Tilemap（带碰撞）
            var wallGO = MakeTilemapLayer(gridObj.transform, "Walls", 1);
            var wallTM = wallGO.GetComponent<Tilemap>();
            var col = wallGO.AddComponent<TilemapCollider2D>();
            wallGO.AddComponent<CompositeCollider2D>();
            col.usedByComposite = true;
            var rb = wallGO.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;

            // 装饰层（可选放家具等）
            var decoGO = MakeTilemapLayer(gridObj.transform, "Decorations", 2);
            var decoTM = decoGO.GetComponent<Tilemap>();

            Tile grassTile = sprGrass != null ? MakeTile(sprGrass) : null;

            // 填充
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    int cell = map[x, y];

                    if (cell == 1)
                    {
                        // 地板
                        Sprite fSpr = floorSprites[Random.Range(0, floorSprites.Count)];
                        floorTM.SetTile(pos, MakeTile(fSpr));
                    }
                    else if (cell == 2)
                    {
                        // 墙壁
                        GetWallSpriteAndRotation(x, y, sprWall, sprCorner, sprCurve, sprEdge,
                            out Sprite wSpr, out float rot);

                        wallTM.SetTile(pos, MakeTile(wSpr));

                        if (Mathf.Abs(rot) > 0.01f)
                        {
                            wallTM.SetTransformMatrix(pos,
                                Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rot), Vector3.one));
                        }
                    }
                    else
                    {
                        // 空地填草
                        if (grassTile != null)
                            floorTM.SetTile(pos, grassTile);
                    }
                }
            }

            // 在部分房间里随机放装饰物
            PlaceDecorations(decoTM, rooms);

            // 出生点
            if (rooms.Count > 0)
            {
                var r = rooms[0];
                var spawn = new GameObject("SpawnPoint");
                spawn.transform.parent = gridObj.transform;
                spawn.transform.position = new Vector3(r.x + r.width / 2f, r.y + r.height / 2f, 0);
            }

            Selection.activeGameObject = gridObj;
        }

        /// <summary>
        /// 在房间中随机放置装饰物
        /// </summary>
        private void PlaceDecorations(Tilemap decoTM, List<RectInt> rooms)
        {
            var decoSprites = new List<Sprite>();
            string[] decoFiles = { "barrel.png", "crate.png", "chest.png", "table.png",
                                   "chair.png", "campfire.png", "bed.png" };
            foreach (var f in decoFiles)
            {
                var spr = LoadSprite(f);
                if (spr != null) decoSprites.Add(spr);
            }
            if (decoSprites.Count == 0) return;

            foreach (var room in rooms)
            {
                // 每个房间放 0~3 个装饰
                int count = Random.Range(0, 4);
                for (int i = 0; i < count; i++)
                {
                    // 不放在边缘（留一格边距）
                    int dx = Random.Range(room.x + 1, room.x + room.width - 1);
                    int dy = Random.Range(room.y + 1, room.y + room.height - 1);
                    var pos = new Vector3Int(dx, dy, 0);
                    if (decoTM.GetTile(pos) == null)
                    {
                        decoTM.SetTile(pos, MakeTile(decoSprites[Random.Range(0, decoSprites.Count)]));
                    }
                }
            }
        }

        private GameObject MakeTilemapLayer(Transform parent, string name, int sortOrder)
        {
            var obj = new GameObject(name);
            obj.transform.parent = parent;
            obj.AddComponent<Tilemap>();
            var tr = obj.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortOrder;
            return obj;
        }

        private Tile MakeTile(Sprite sprite)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            return t;
        }

        private Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(tilePath + fileName);
        }

        #endregion

        private void ClearDungeon()
        {
            var existing = GameObject.Find("DungeonGrid");
            if (existing != null) Undo.DestroyObjectImmediate(existing);
        }
    }
}
