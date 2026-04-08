using UnityEngine;
using System.Text.RegularExpressions;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Uses a specified text color for subtitle lines spoken by the actor.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class ActorSubtitleColor : MonoBehaviour
    {

        /// <summary>
        /// The color to use for subtitle lines spoken by this actor.
        /// </summary>
        [Tooltip("此角色所说字幕行的颜色。")]
        public Color color = Color.white;

        public enum ApplyTo { DialogueText, PrependedActorName }

        [Tooltip("将颜色应用到整个对话文本，或在前面添加角色名并只给名字上色。")]
        public ApplyTo applyTo = ApplyTo.DialogueText;

        [Tooltip("如果在前面添加角色名，则用此字符串与 Dialogue Text 分隔。")]
        public string prependActorNameSeparator = ": ";

        public void OnConversationLine(Subtitle subtitle)
        {
            CheckSubtitle(subtitle);
        }

        public void OnBarkLine(Subtitle subtitle)
        {
            CheckSubtitle(subtitle);
        }

        private void CheckSubtitle(Subtitle subtitle)
        {
            if (subtitle != null && subtitle.speakerInfo != null && subtitle.speakerInfo.transform == this.transform)
            {
                subtitle.formattedText.text = ProcessText(subtitle);
            }
        }

        private string ProcessText(Subtitle subtitle)
        {
            switch (applyTo)
            {
                default:
                case ApplyTo.DialogueText:
                    return UITools.WrapTextInColor(subtitle.formattedText.text, color);
                case ApplyTo.PrependedActorName:
                    return UITools.WrapTextInColor(subtitle.speakerInfo.Name + prependActorNameSeparator, color) + subtitle.formattedText.text;
            }
        }

    }
}
