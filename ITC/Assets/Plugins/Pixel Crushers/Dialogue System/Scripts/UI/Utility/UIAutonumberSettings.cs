// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class UIAutonumberSettings
    {
        [Tooltip("启用回应自动编号。")]
        public bool enabled = false;

        [Tooltip("将普通数字键绑定为热键。")]
        public bool regularNumberHotkeys = true;

        [Tooltip("将小键盘数字键绑定为热键。")]
        public bool numpadHotkeys = false;

        [Tooltip("回应按钮文本的格式，其中 {0} 是热键编号，{1} 是菜单文本。")]
        public string format = "{0}. {1}";
    }

}
