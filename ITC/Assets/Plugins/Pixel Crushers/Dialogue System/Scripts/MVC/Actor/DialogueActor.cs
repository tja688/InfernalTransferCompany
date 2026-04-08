// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This component allows you to override the actor name used in conversations,
    /// which is normally set to the name of the GameObject. If the override name
    /// contains a [lua] or [var] tag, it parses the value.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class DialogueActor : MonoBehaviour
    {

        /// <summary>
        /// Overrides the actor name used in conversations.
        /// </summary>
        [Tooltip("在对话中使用此角色名称。")]
        [ActorPopup(true)]
        [UnityEngine.Serialization.FormerlySerializedAs("overrideName")]
        public string actor;

        /// <summary>
        /// The internal name to use in the dialogue database when saving persistent data.
        /// If blank, uses the override name.
        /// </summary>
        [Tooltip("保存持久化数据时使用的名称。若为空，则使用角色名称。")]
        [UnityEngine.Serialization.FormerlySerializedAs("internalName")]
        public string persistentDataName;

        [Tooltip("可选肖像。如果未指定，将使用数据库中该角色的肖像。此字段允许你指定一个 Texture。")]
        public Texture2D portrait;

        [Tooltip("可选肖像。如果未指定，将使用数据库中角色的肖像。此字段可让你指定一个 Sprite。")]
        public Sprite spritePortrait;

        [Tooltip("此角色的自定义摄像机角度。如果已指定，将覆盖 Dialogue Manager 的 Camera & Cutscene Settings > Camera Angles。")]
        public GameObject cameraAngles;

        [Tooltip("可选。指定与 Audio()、AudioWait() 等 sequencer 命令一起使用的 Audio Source。")]
        public AudioSource audioSource;

        [Serializable]
        public class BarkUISettings
        {
            [Tooltip("如果是 prefab，Dialogue Actor 会在运行时实例化它。")]
            public AbstractBarkUI barkUI;

            [Tooltip("如果实例化 bark UI prefab，则相对于 Dialogue Actor 的原点偏移这么远。")]
            public Vector3 barkUIOffset = new Vector3(0, 2, 0);
        }

        public BarkUISettings barkUISettings = new BarkUISettings();

        public enum UseMenuPanelFor { OnlyMe, MeAndResponsesToMe }

        [Serializable]
        public class StandardDialogueUISettings
        {
            [Tooltip("如果使用 Standard Dialogue UI，则为此角色使用的字幕面板。")]
            public SubtitlePanelNumber subtitlePanelNumber = SubtitlePanelNumber.Default;

            [Tooltip("当 Subtitle Panel Number 设为 Custom 时要使用的面板。")]
            public StandardUISubtitlePanel customSubtitlePanel = null;

            [Tooltip("如果实例化字幕面板 prefab，则相对于 Dialogue Actor 的原点偏移这么远。")]
            public Vector3 customSubtitlePanelOffset = new Vector3(0, 0, 0);

            [Tooltip("如果使用 Standard Dialogue UI，则为此角色使用的菜单面板。")]
            public MenuPanelNumber menuPanelNumber = MenuPanelNumber.Default;

            [Tooltip("当 Menu Panel Number 设为 Custom 时要使用的面板。")]
            public StandardUIMenuPanel customMenuPanel = null;

            [Tooltip("如果实例化菜单面板 prefab，则相对于 Dialogue Actor 的原点偏移这么远。")]
            public Vector3 customMenuPanelOffset = new Vector3(0, 0, 0);

            [Tooltip("如果是 Only Me，则仅当此 Dialogue Actor 是回应者时使用此菜单面板。\n如果是 MeAndResponsesToMe，则当此 Dialogue Actor 是回应者或被回应角色（即最后一个发言者）时使用此菜单面板。")]
            public UseMenuPanelFor useMenuPanelFor = UseMenuPanelFor.OnlyMe;

            [Tooltip("如果已指定，则为运行此角色动画肖像的 Animator Controller。它应驱动 Image 组件，而不是 SpriteRenderer。")]
            public RuntimeAnimatorController portraitAnimatorController;

            [Tooltip("为此角色指定字幕颜色。")]
            public bool setSubtitleColor = false;

            [Tooltip("在前面加上角色名，并且只对名字应用颜色。")]
            public bool applyColorToPrependedName = false;

            [Tooltip("如果在前面添加角色名，则用此字符串与 Dialogue Text 分隔。")]
            public string prependActorNameSeparator = ": ";

            [Tooltip("如果在前面添加角色名，则按此方式格式化，其中 {0} 是名字 + 分隔符，{1} 是 Dialogue Text。")]
            public string prependActorNameFormat = "{0}{1}";

            [Tooltip("此角色字幕使用的颜色。")]
            public Color subtitleColor = Color.white;
        }

        public StandardDialogueUISettings standardDialogueUISettings = new StandardDialogueUISettings();

        protected virtual void Awake()
        {
            SetupBarkUI();
            SetupDialoguePanels();
        }

        public virtual Sprite GetPortraitSprite()
        {
            return UITools.GetSprite(portrait, spritePortrait);
        }

        protected virtual void SetupBarkUI()
        {
            if (barkUISettings.barkUI != null && Tools.IsPrefab(barkUISettings.barkUI.gameObject))
            {
                // Instantiate bark UI from prefab:
                var go = Instantiate(barkUISettings.barkUI.gameObject) as GameObject;
                go.transform.SetParent(transform);
                go.transform.localPosition = barkUISettings.barkUIOffset;
                go.transform.localRotation = Quaternion.identity;
                barkUISettings.barkUI = go.GetComponent<AbstractBarkUI>();
            }
        }

        protected virtual void SetupDialoguePanels()
        {
            if (standardDialogueUISettings.subtitlePanelNumber == SubtitlePanelNumber.Custom &&
                standardDialogueUISettings.customSubtitlePanel != null &&
                Tools.IsPrefab(standardDialogueUISettings.customSubtitlePanel.gameObject))
            {
                // Instantiate subtitle panel from prefab:
                var go = Instantiate(standardDialogueUISettings.customSubtitlePanel.gameObject, transform.position, transform.rotation) as GameObject;
                go.transform.SetParent(transform);
                go.transform.localPosition = standardDialogueUISettings.customSubtitlePanelOffset;
                go.transform.localRotation = Quaternion.identity;
                standardDialogueUISettings.customSubtitlePanel = go.GetComponent<StandardUISubtitlePanel>();
            }
            if (standardDialogueUISettings.menuPanelNumber == MenuPanelNumber.Custom &&
                standardDialogueUISettings.customMenuPanel != null &&
                Tools.IsPrefab(standardDialogueUISettings.customMenuPanel.gameObject))
            {
                // Instantiate menu panel from prefab:
                var go = Instantiate(standardDialogueUISettings.customMenuPanel.gameObject, transform.position, transform.rotation) as GameObject;
                go.transform.SetParent(transform);
                go.transform.localPosition = standardDialogueUISettings.customMenuPanelOffset;
                go.transform.localRotation = Quaternion.identity;
                standardDialogueUISettings.customMenuPanel = go.GetComponent<StandardUIMenuPanel>();
            }
        }

        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(actor)) return;
            StartCoroutine(RegisterAtEndOfFrame());
        }

        /// <summary>
        /// Immediately registers as a candidate for the actor.
        /// Then waits for end of frame in case DialogueActor's parent is destroyed on 
        /// same frame but after OnEnable(). Must do this because OnEnable() can run 
        /// before another GameObject's Awake() method. If it survives to the end
        /// of frame, registers it as the actual actor.
        /// </summary>
        protected IEnumerator RegisterAtEndOfFrame()
        {
            CharacterInfo.RegisterCandidateActorTransform(actor, transform);
            yield return new WaitForEndOfFrame();
            CharacterInfo.RegisterActorTransform(actor, transform);
        }

        protected virtual void OnDisable()
        {
            if (string.IsNullOrEmpty(actor)) return;
            CharacterInfo.UnregisterCandidateActorTransform(actor, transform);
            var registeredTransform = CharacterInfo.GetRegisteredActorTransform(actor);
            if (transform == registeredTransform)
            {
                CharacterInfo.UnregisterActorTransform(actor, transform);

                // If a conversation is active, remove this actor from its model's character cache:
                if (DialogueManager.isConversationActive)
                {
                    var actorAsset = DialogueManager.masterDatabase.GetActor(actor);
                    if (actorAsset != null)
                    {
                        foreach (var activeConversation in DialogueManager.instance.activeConversations)
                        {
                            activeConversation.conversationModel.ClearCharacterInfo(actorAsset.id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deprecated alias for GetActorName.
        /// </summary>
        public virtual string GetName()
        {
            return GetActorName();
        }

        /// <summary>
        /// Gets the name to use for this DialogueActor, including parsing if it contains a [lua],
        /// [var], or [em#] tag.
        /// </summary>
        /// <returns>The name to use, or <c>null</c> if not set.</returns>
        public virtual string GetActorName()
        {
            var actorName = string.IsNullOrEmpty(actor) ? name : actor;
            var result = CharacterInfo.GetLocalizedDisplayNameInDatabase(DialogueLua.GetActorField(actorName, "Name").asString);
            if (!string.IsNullOrEmpty(result)) actorName = result;
            if (actorName.Contains("[lua") || actorName.Contains("[var") || actorName.Contains("[em"))
            {
                return FormattedText.Parse(actorName, DialogueManager.masterDatabase.emphasisSettings).text;
            }
            else
            {
                return actorName;
            }
        }

        /// <summary>
        /// Gets the name to use when saving persistent data.
        /// </summary>
        public virtual string GetPersistentDataName()
        {
            return string.IsNullOrEmpty(persistentDataName) ? GetActorName() : persistentDataName;
        }

        /// <summary>
        /// Gets the panel number to use if using a Standard Dialogue UI.
        /// </summary>
        public virtual SubtitlePanelNumber GetSubtitlePanelNumber()
        {
            return standardDialogueUISettings.subtitlePanelNumber;
        }

        /// <summary>
        /// Changes a dialogue actor's subtitle panel number. If a conversation is active, updates
        /// the dialogue UI.
        /// </summary>
        public virtual void SetSubtitlePanelNumber(SubtitlePanelNumber newSubtitlePanelNumber)
        {
            standardDialogueUISettings.subtitlePanelNumber = newSubtitlePanelNumber;
            if (DialogueManager.isConversationActive && DialogueManager.dialogueUI is IStandardDialogueUI)
            {
                (DialogueManager.dialogueUI as IStandardDialogueUI).SetActorSubtitlePanelNumber(this, newSubtitlePanelNumber);
            }
        }

        /// <summary>
        /// Gets the menu panel number to use if using a Standard Dialogue UI.
        /// </summary>
        public virtual MenuPanelNumber GetMenuPanelNumber()
        {
            return standardDialogueUISettings.menuPanelNumber;
        }

        /// <summary>
        /// Changes a dialogue actor's menu panel number. If a conversation is active, updates
        /// the dialogue UI.
        /// </summary>
        public virtual void SetMenuPanelNumber(MenuPanelNumber newMenuPanelNumber)
        {
            standardDialogueUISettings.menuPanelNumber = newMenuPanelNumber;
            if (DialogueManager.isConversationActive && DialogueManager.dialogueUI is IStandardDialogueUI)
            {
                (DialogueManager.dialogueUI as IStandardDialogueUI).SetActorMenuPanelNumber(this, newMenuPanelNumber);
            }
        }

        /// <summary>
        /// Applies any color settings specified in the actor's standardDialogueUISettings.
        /// </summary>
        /// <param name="subtitle">The subtitle containing the source text.</param>
        /// <returns>The subtitle text adjusted for the actor's color settings.</returns>
        public virtual string AdjustSubtitleColor(Subtitle subtitle)
        {
            var text = subtitle.formattedText.text;
            if (!standardDialogueUISettings.setSubtitleColor)
            {
                return text;
            }
            if (standardDialogueUISettings.applyColorToPrependedName)
            {
                if (string.IsNullOrEmpty(subtitle.speakerInfo.Name))
                {
                    return text;
                }
                else
                {
                    //return UITools.WrapTextInColor(subtitle.speakerInfo.Name + standardDialogueUISettings.prependActorNameSeparator, standardDialogueUISettings.subtitleColor) + text;
                    var coloredName = UITools.WrapTextInColor(subtitle.speakerInfo.Name + standardDialogueUISettings.prependActorNameSeparator, standardDialogueUISettings.subtitleColor);
                    var s = string.Format(standardDialogueUISettings.prependActorNameFormat, new object[] { coloredName, text });
                    return FormattedText.Parse(s).text;
                }
            }
            else
            {
                return UITools.WrapTextInColor(text, standardDialogueUISettings.subtitleColor);
            }
        }

        /// <summary>
        /// Searches a GameObject, including its parents and children, for a DialogueActor component.
        /// </summary>
        /// <param name="t">The GameObject to search.</param>
        /// <returns>The DialogueActor component, or null if not found.</returns>
        public static DialogueActor GetDialogueActorComponent(Transform t)
        {
            if (t == null) return null;
            return t.GetComponent<DialogueActor>() ?? t.GetComponentInChildren<DialogueActor>() ?? t.GetComponentInParent<DialogueActor>();
        }

        /// <summary>
        /// Gets the name of the actor, either from the GameObject or its DialogueActor
        /// if present.
        /// </summary>
        /// <returns>The actor name.</returns>
        /// <param name="t">The actor's transform.</param>
        public static string GetActorName(Transform t)
        {
            if (t == null) return string.Empty;
            var dialogueActor = GetDialogueActorComponent(t);
            return (dialogueActor != null && dialogueActor.isActiveAndEnabled) ? dialogueActor.GetName()
                : CharacterInfo.GetLocalizedDisplayNameInDatabase(t.name);
        }

        /// <summary>
        /// Gets the persistent data name of the actor, from the DialogueActor's persistentDataName
        /// if set, otherwise the actor name, or the GameObject name if the GameObject doesn't have a
        /// DialogueActor component.
        /// </summary>
        /// <param name="t">The actor's transform.</param>
        /// <returns></returns>
        public static string GetPersistentDataName(Transform t)
        {
            if (t == null) return string.Empty;
            var dialogueActor = GetDialogueActorComponent(t);
            if (dialogueActor != null)
            {
                if (!string.IsNullOrEmpty(dialogueActor.persistentDataName)) return dialogueActor.persistentDataName;
                if (!string.IsNullOrEmpty(dialogueActor.actor)) return dialogueActor.actor;
            }
            return t.name;
        }

        /// <summary>
        /// Gets the panel number to use if using a Standard Dialogue UI.
        /// </summary>
        /// <param name="t">The actor's transform.</param>
        public static SubtitlePanelNumber GetSubtitlePanelNumber(Transform t)
        {
            var dialogueActor = GetDialogueActorComponent(t);
            return (dialogueActor != null) ? dialogueActor.GetSubtitlePanelNumber() : SubtitlePanelNumber.Default;
        }

        public static IBarkUI GetBarkUI(Transform t)
        {
            if (t == null) return null;
            var dialogueActor = GetDialogueActorComponent(t);
            return (dialogueActor != null) ? (dialogueActor.barkUISettings.barkUI as IBarkUI) : t.GetComponentInChildren(typeof(IBarkUI)) as IBarkUI;
        }

    }

}
