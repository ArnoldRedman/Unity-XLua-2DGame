#if !XLUA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SkierFramework
{
    /// <summary>
    /// 背包列表项 C# 薄壳
    /// 继承 UILoopItem，由 UIScrollView 管理复用
    /// </summary>
    public class UIBagItem : UILoopItem
    {
        [ControlBinding] private TextMeshProUGUI TxtName;
        [ControlBinding] private TextMeshProUGUI TxtCount;
        [ControlBinding] private Image ImgIcon;
        [ControlBinding] private Image ImgQuality;
        [ControlBinding] private Button BtnItem;

        private BagItemData _data;

        public override void OnInit()
        {
            base.OnInit();
        }

        public override void OnAddListener()
        {
            base.OnAddListener();
            if (BtnItem != null)
            {
                BtnItem.AddClick(OnClickItem);
            }
        }

        protected override void OnUpdateData(IList dataList, int index, object userData)
        {
            if (index < 0 || index >= dataList.Count) return;

            _data = dataList[index] as BagItemData;
            if (_data == null) return;

            if (TxtName != null)
                TxtName.text = _data.name;

            if (TxtCount != null)
                TxtCount.text = _data.count > 1 ? _data.count.ToString() : "";

            // 品质颜色
            if (ImgQuality != null)
            {
                ImgQuality.color = GetQualityColor(_data.quality);
            }

            // 图标加载（通过Addressables异步加载）
            if (ImgIcon != null && !string.IsNullOrEmpty(_data.icon))
            {
                // 简化处理：实际项目中应通过 ResourceManager 异步加载图标
                // ResourceManager.Instance.LoadAssetAsync<Sprite>(iconPath, (sprite) => { ImgIcon.sprite = sprite; });
            }
        }

        public override void CheckSelect(int index)
        {
            // 可以在这里做选中高亮效果
        }

        private void OnClickItem()
        {
            if (_data == null) return;

            // 发事件给 Lua 层处理
#if XLUA
            var luaEnv = LuaEnvManager.Instance?.LuaEnv;
            if (luaEnv != null)
            {
                luaEnv.DoString(
                    $"EventBus:Emit('BagItemClicked', {_data.slotIndex}, {_data.itemId})",
                    "BagItemClick");
            }
#endif
        }

        private Color GetQualityColor(int quality)
        {
            switch (quality)
            {
                case 1: return Color.white;
                case 2: return Color.green;
                case 3: return new Color(0.3f, 0.5f, 1f);  // 蓝
                case 4: return new Color(0.7f, 0.3f, 0.9f); // 紫
                case 5: return new Color(1f, 0.6f, 0f);      // 橙
                default: return Color.gray;
            }
        }
    }
}
#endif
