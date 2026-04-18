using UnityEngine;

namespace SkierFramework
{
    /// <summary>
    /// 2D 顶视角角色控制器
    /// 挂在角色 GameObject 上，需要 Rigidbody2D
    /// 支持 WASD / 方向键移动，4方向动画切换
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("动画参数名（可选，没有 Animator 也能跑）")]
        [SerializeField] private string animMoveX = "MoveX";
        [SerializeField] private string animMoveY = "MoveY";
        [SerializeField] private string animIsMoving = "IsMoving";

        private Rigidbody2D _rb;
        private Animator _animator;
        private Vector2 _moveInput;
        private Vector2 _lastDirection = Vector2.down;

        /// <summary>
        /// 是否允许移动（UI 打开时可禁止）
        /// </summary>
        public bool CanMove { get; set; } = true;

        /// <summary>
        /// 当前朝向（用于 NPC 交互判定方向等）
        /// </summary>
        public Vector2 FacingDirection => _lastDirection;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.freezeRotation = true;

            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (!CanMove)
            {
                _moveInput = Vector2.zero;
                return;
            }

            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _lastDirection = _moveInput.normalized;
            }

            UpdateAnimation();

            // 快捷键
            if (Input.GetKeyDown(KeyCode.B))
            {
                UIManager.Instance.Open(UIType.UIBagView);
            }
        }

        private void FixedUpdate()
        {
            _rb.velocity = _moveInput.normalized * moveSpeed;
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            bool isMoving = _moveInput.sqrMagnitude > 0.01f;
            _animator.SetBool(animIsMoving, isMoving);
            _animator.SetFloat(animMoveX, _lastDirection.x);
            _animator.SetFloat(animMoveY, _lastDirection.y);
        }

        /// <summary>
        /// 外部强制停止移动（如对话时）
        /// </summary>
        public void StopMovement()
        {
            CanMove = false;
            _moveInput = Vector2.zero;
            _rb.velocity = Vector2.zero;
            UpdateAnimation();
        }

        /// <summary>
        /// 恢复移动
        /// </summary>
        public void ResumeMovement()
        {
            CanMove = true;
        }
    }
}
