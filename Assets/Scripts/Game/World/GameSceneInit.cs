using UnityEngine;

namespace SkierFramework
{
    /// <summary>
    /// Game 场景初始化脚本
    /// 放在 Game 场景的一个空 GameObject 上
    /// 负责生成角色、设置摄像机跟随
    /// </summary>
    public class GameSceneInit : MonoBehaviour
    {
        [Header("角色预制体（可以从 Addressables 加载，也可以直接拖）")]
        [SerializeField] private GameObject playerPrefab;

        [Header("出生点")]
        [SerializeField] private Transform spawnPoint;

        private void Start()
        {
            SpawnPlayer();
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[GameSceneInit] playerPrefab is null! 请在 Inspector 中拖入角色预制体。");
                return;
            }

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            playerObj.name = "Player";

            // 设置摄像机跟随
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<CameraFollow>();
                if (follow == null)
                {
                    follow = cam.gameObject.AddComponent<CameraFollow>();
                }
                follow.SetTarget(playerObj.transform);
            }
        }
    }
}
