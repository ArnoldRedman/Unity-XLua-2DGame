using UnityEngine;
#if XLUA
using XLua;
#endif

namespace SkierFramework
{
    /// <summary>
    /// NPC 交互控制器
    /// 挂在 NPC GameObject 上，需要 Collider2D (Trigger)
    /// 玩家进入范围后按 E 键触发交互，调用 Lua 对话系统
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCController : MonoBehaviour
    {
        [Header("NPC 配置")]
        [Tooltip("NPC 唯一 ID")]
        public int npcId;

        [Tooltip("NPC 名字（显示用）")]
        public string npcName = "NPC";

        [Tooltip("默认对话 ID")]
        public int dialogueId = 1;

        [Header("交互提示")]
        [SerializeField] private GameObject interactHint;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private bool _playerInRange;
        private bool _isInDialogue;

        private void Awake()
        {
            // 确保 collider 是 trigger
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            if (interactHint != null)
                interactHint.SetActive(false);
        }

        private void Update()
        {
            if (_playerInRange && !_isInDialogue && Input.GetKeyDown(interactKey))
            {
                StartInteraction();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                _playerInRange = true;
                if (interactHint != null) interactHint.SetActive(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                _playerInRange = false;
                if (interactHint != null) interactHint.SetActive(false);
            }
        }

        private void StartInteraction()
        {
            _isInDialogue = true;

            // 冻结玩家移动
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.StopMovement();

#if XLUA
            // 查询 Lua NPC 配置获取动态对话 ID（根据任务状态）
            // Lua 端: NpcDialogueMap[npcId] 函数返回当前对话 ID
            var luaEnv = LuaEnvManager.Instance?.LuaEnv;
            if (luaEnv != null)
            {
                // 尝试获取动态对话 ID，失败则用默认值
                var results = luaEnv.DoString(
                    string.Format("if NpcDialogueMap and NpcDialogueMap[{0}] then return NpcDialogueMap[{0}]() else return {1} end", npcId, dialogueId),
                    "NPCGetDialogue");
                int actualDialogueId = dialogueId;
                if (results != null && results.Length > 0 && results[0] is long v)
                    actualDialogueId = (int)v;
                else if (results != null && results.Length > 0 && results[0] is double d)
                    actualDialogueId = (int)d;

                luaEnv.DoString(
                    string.Format("DialogueSystem:StartDialogue({0}, '{1}')", actualDialogueId, npcName.Replace("'", "\\'")),
                    "NPCInteract");
            }
#endif

            // 监听对话结束事件
#if XLUA
            var luaEnv2 = LuaEnvManager.Instance?.LuaEnv;
            if (luaEnv2 != null)
            {
                luaEnv2.DoString(
                    string.Format("EventBus:On('DialogueEnd', function() " +
                        "local go = CS.UnityEngine.GameObject.Find('{0}'); " +
                        "if go then local npc = go:GetComponent(typeof(CS.SkierFramework.NPCController)); " +
                        "if npc then npc:OnDialogueEnd() end end; " +
                        "EventBus:Off('DialogueEnd') end)",
                        gameObject.name.Replace("'", "\\'")),
                    "NPCDialogueEndHook");
            }
#endif
        }

        /// <summary>
        /// 对话结束回调（从 Lua 调用）
        /// </summary>
        public void OnDialogueEnd()
        {
            _isInDialogue = false;
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.ResumeMovement();
        }
    }
}
