// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Display settings to apply to the dialogue UI and sequencer.
    /// </summary>
    [System.Serializable]
    public class DisplaySettings
    {

        public DisplaySettings() { }

        public DisplaySettings(DisplaySettings source)
        {
            this.conversationOverrideSettings = source.conversationOverrideSettings;
            this.dialogueUI = source.dialogueUI;
            this.defaultCanvas = source.defaultCanvas;
            this.localizationSettings = new LocalizationSettings(source.localizationSettings);
            this.subtitleSettings = new SubtitleSettings(source.subtitleSettings);
            this.cameraSettings = new CameraSettings(source.cameraSettings);
            this.inputSettings = new InputSettings(source.inputSettings);
            this.barkSettings = new BarkSettings(source.barkSettings);
            this.alertSettings = new AlertSettings(source.alertSettings);
        }

        [HideInInspector]
        public ConversationOverrideDisplaySettings conversationOverrideSettings = null;

        [Tooltip("指定一个包含激活的 dialogue UI 组件的 GameObject。可以是 prefab。若未指定，Dialogue Manager 会在其子对象中搜索激活的 dialogue UI 组件。")]
        public GameObject dialogueUI;

        [Tooltip("可选。若 dialogue UI 是 prefab，请指定要实例化到其中的 Canvas。")]
        public Canvas defaultCanvas;

        [System.Serializable]
        public class LocalizationSettings
        {

            public LocalizationSettings() { }

            public LocalizationSettings(LocalizationSettings source)
            {
                this.language = source.language;
                this.useSystemLanguage = source.useSystemLanguage;
                this.textTable = source.textTable;
            }

            /// <summary>
            /// The current language, or blank to use the default language.
            /// </summary>
            [Tooltip("当前语言，留空则使用默认语言。")]
            public string language = string.Empty;

            /// <summary>
            /// Set <c>true</c> to automatically use the system language at startup.
            /// </summary>
            [Tooltip("启动时使用系统语言。")]
            public bool useSystemLanguage = false;

            /// <summary>
            /// An optional text table. Used by DialogueSystemController.GetLocalizedText()
            /// and ShowAlert() if assigned.
            /// </summary>
            [Tooltip("用于 alerts 和其他通用文本的可选本地化文本。注意：现在使用 Text Table，而不是 Localized Text Table。")]
            public TextTable textTable = null;
            //---Was: public LocalizedTextTable localizedText = null;
        }

        public LocalizationSettings localizationSettings = new LocalizationSettings();

        [System.Serializable]
        public class SubtitleSettings
        {
            public SubtitleSettings() { }

            public SubtitleSettings(SubtitleSettings source)
            {
                this.showNPCSubtitlesDuringLine = source.showNPCSubtitlesDuringLine;
                this.showNPCSubtitlesWithResponses = source.showNPCSubtitlesWithResponses;
                this.showPCSubtitlesDuringLine = source.showPCSubtitlesDuringLine;
                this.allowPCSubtitleReminders = source.allowPCSubtitleReminders;
                this.skipPCSubtitleAfterResponseMenu = source.skipPCSubtitleAfterResponseMenu;
                this.subtitleCharsPerSecond = source.subtitleCharsPerSecond;
                this.minSubtitleSeconds = source.minSubtitleSeconds;
                this.continueButton = source.continueButton;
                this.requireContinueOnLastLine = source.requireContinueOnLastLine;
                this.richTextEmphases = source.richTextEmphases;
                this.convertPipesToLineBreaks = source.convertPipesToLineBreaks;
                this.informSequenceStartAndEnd = source.informSequenceStartAndEnd;
            }

            /// <summary>
            /// Specifies whether to show NPC subtitles while speaking a line of dialogue.
            /// </summary>
            [Tooltip("当 NPC 说话时显示 NPC subtitle 文本。")]
            public bool showNPCSubtitlesDuringLine = true;

            /// <summary>
            /// Specifies whether to should show NPC subtitles while presenting the player's follow-up
            /// responses.
            /// </summary>
            [Tooltip("在显示玩家 Response Menu 时显示 NPC subtitle 提示文本。如果你使用 Standard Dialogue UI，则 subtitle panel 的 Visiblity 值优先于此。")]
            public bool showNPCSubtitlesWithResponses = true;

            /// <summary>
            /// Specifies whether to show PC subtitles while speaking a line of dialogue.
            /// </summary>
            [Tooltip("当 PC 说话时显示 PC subtitle 文本。如果勾选下方的 Skip PC Subtitle After Response Menu，则来自 Response Menu 选择的 PC subtitles 将被跳过。")]
            public bool showPCSubtitlesDuringLine = false;

            /// <summary>
            /// Set <c>true</c> to allow PC subtitles to be used for the reminder line
            /// during the response menu.
            /// </summary>
            [Tooltip("在显示 Response Menu 时，允许使用 PC subtitles 作为提示文本。")]
            public bool allowPCSubtitleReminders = false;

            /// <summary>
            /// If the PC's subtitle came from a response menu selection, don't show the subtitle even if showPCSubtitlesDuringLine is true.
            /// </summary>
            [Tooltip("如果 PC 的 subtitle 来自 Response Menu 选择，即使勾选 Show PC Subtitles During Line，也不要显示该 subtitle。")]
            public bool skipPCSubtitleAfterResponseMenu = false;

            /// <summary>
            /// The default subtitle characters per second. This value is used to compute the default 
            /// duration to display a subtitle if no sequence is specified for a line of dialogue.
            /// This value is also used when displaying alerts.
            /// </summary>
            [Tooltip("用于计算 subtitle 的默认显示时长。Typewriter effects 有各自独立的设置。")]
            public float subtitleCharsPerSecond = 30f;

            /// <summary>
            /// The minimum duration to display a subtitle if no sequence is specified for a line of 
            /// dialogue. This value is also used when displaying alerts.
            /// </summary>
            [Tooltip("subtitle 的默认最小时长。")]
            public float minSubtitleSeconds = 2f;

            public enum ContinueButtonMode
            {
                /// <summary>
                /// Never wait for the continue button. Use this if your UI doesn't have continue buttons.
                /// </summary>
                Never,

                /// <summary>
                /// Always wait for the continue button.
                /// </summary>
                Always,

                /// <summary>
                /// Show the continue button but don't wait for it.
                /// </summary>
                Optional,

                /// <summary>
                /// Wait for the continue button, except when the response menu is next show but don't wait.
                /// </summary>
                OptionalBeforeResponseMenu,

                /// <summary>
                /// Wait for the continue button, except when the response menu is next hide it.
                /// </summary>
                NotBeforeResponseMenu,

                /// <summary>
                /// Wait for the continue button, except when a PC auto-select response or response
                /// menu is next, show but don't wait.
                /// </summary>
                OptionalBeforePCAutoresponseOrMenu,

                /// <summary>
                /// Wait for the continue button, except with a PC auto-select response or response
                /// menu is next, hide it.
                /// </summary>
                NotBeforePCAutoresponseOrMenu,

                /// <summary>
                /// Wait for the continue button, except when delivering PC lines show but don't wait.
                /// </summary>
                OptionalForPC,

                /// <summary>
                /// Wait for the continue button except when delivering PC lines.
                /// </summary>
                NotForPC,

                /// <summary>
                /// Wait for the continue button, except when preceding response menus or delivering PC lines don't wait.
                /// </summary>
                OptionalForPCOrBeforeResponseMenu,

                /// <summary>
                /// Wait for the continue button only for NPC lines that don't precede response menus.
                /// </summary>
                NotForPCOrBeforeResponseMenu,

                /// <summary>
                /// Wait for the continue button, except for PC lines and lines preceding a response menu or PC auto-select response don't wait.
                /// </summary>
                OptionalForPCOrBeforePCAutoresponseOrMenu,

                /// <summary>
                /// Wait for the continue button only for NPC lines that don't precede response menus or PC auto-select responses.
                /// </summary>
                NotForPCOrBeforePCAutoresponseOrMenu,

                /// <summary>
                /// Wait for continue button for PC lines but not for NPC lines.
                /// </summary>
                OnlyForPC
            }

            /// <summary>
            /// How to handle continue buttons.
            /// </summary>
            [Tooltip("继续按钮的处理方式。")]
            public ContinueButtonMode continueButton = ContinueButtonMode.Never;

            [Tooltip("如果勾选，则在结束对话的字幕上始终要求显示继续按钮。会覆盖上方的 Continue Button 下拉选项。")]
            public bool requireContinueOnLastLine = false;

            /// <summary>
            /// Set <c>true</c> to convert "[em#]" tags to rich text codes in formatted text.
            /// Your implementation of IDialogueUI must support rich text.
            /// </summary>
            [Tooltip("对 [em#] 标记使用富文本代码。若未勾选，[em#] 标签会将颜色应用到整段文本。")]
            public bool richTextEmphases = true;

            /// <summary>
            /// Treat '|' characters in text as line breaks.
            /// </summary>
            [Tooltip("将文本中的 '|' 字符视为换行。")]
            public bool convertPipesToLineBreaks = true;

            /// <summary>
            /// Set <c>true</c> to send OnSequenceStart and OnSequenceEnd messages with 
            /// every dialogue entry's sequence.
            /// </summary>
            [Tooltip("对每个对话条目的 Sequence 都发送 OnSequenceStart 和 OnSequenceEnd 消息。")]
            public bool informSequenceStartAndEnd = false;
        }

        /// <summary>
        /// The subtitle settings.
        /// </summary>
        public SubtitleSettings subtitleSettings = new SubtitleSettings();

        [System.Serializable]
        public class CameraSettings
        {
            public CameraSettings() { }

            public CameraSettings(CameraSettings source)
            {
                this.sequencerCamera = source.sequencerCamera;
                this.alternateCameraObject = source.alternateCameraObject;
                this.cameraAngles = source.cameraAngles;
                this.keepCameraPositionAtConversationEnd = source.keepCameraPositionAtConversationEnd;
                this.cameraEasing = Tweener.Easing.Linear;
                this.showSubtitleOnEmptyContinue = source.showSubtitleOnEmptyContinue;
                this.defaultSequence = source.defaultSequence;
                this.defaultPlayerSequence = source.defaultPlayerSequence;
                this.defaultResponseMenuSequence = source.defaultResponseMenuSequence;
                this.entrytagFormat = source.entrytagFormat;
                this.treatAllCommandsAsRequired = source.treatAllCommandsAsRequired;
                this.reportMissingAudioFiles = source.reportMissingAudioFiles;
                this.disableInternalSequencerCommands = source.disableInternalSequencerCommands;
            }

            /// <summary>
            /// The camera (or prefab) to use for sequences. If unassigned, the sequencer will use the
            /// main camera; when the sequence is done, it will restore the main camera's original
            /// position.
            /// </summary>
            [Tooltip("Sequence 使用的 Camera 或 prefab。若未指定，Sequence 将使用当前主 camera。")]
            public Camera sequencerCamera = null;

            /// <summary>
            /// An alternate camera object to use instead of sequencerCamera. Use this, for example,
            /// if you have an Oculus VR GameObject that's a parent of two cameras.  Currently this 
            /// <em>must</em> be an object in the scene, not a prefab.
            /// </summary>
            [Tooltip("如果已指定，则改用它而不是 Sequencer Camera，例如 Oculus VR GameObject。不能是 prefab。")]
            public GameObject alternateCameraObject = null;

            /// <summary>
            /// The camera angle object (or prefab) to use for the "Camera()" sequence command. See
            /// @ref sequencerCommandCamera for more information.
            /// </summary>
            [Tooltip("Camera angle 对象或 prefab。若未指定，则使用默认的 camera angle 定义。")]
            public GameObject cameraAngles = null;

            /// <summary>
            /// Specifies how Camera() commands should move the camera.
            /// </summary>
            [Tooltip("指定 Camera() 命令应如何移动 camera。")]
            public Tweener.Easing cameraEasing = Tweener.Easing.Linear;

            /// <summary>
            /// If conversation's sequences use Main Camera, leave camera in current position at end of conversation instead of restoring pre-conversation position.
            /// </summary>
            [Tooltip("如果对话的 Sequence 使用 Main Camera，则在对话结束时保留 camera 的当前位置，而不是恢复对话前的位置。")]
            public bool keepCameraPositionAtConversationEnd = false;

            /// <summary>
            /// Show subtitle if sequence is only 'Continue()'. Typically only useful in UIs that accumulate text.
            /// </summary>
            [Tooltip("如果 Sequence 仅为 'Continue()'，则显示 subtitle。通常只对会累积文本的 UI 有用。")]
            public bool showSubtitleOnEmptyContinue = false;

            /// <summary>
            /// The default sequence to use if the dialogue entry doesn't have a sequence defined 
            /// in its Sequence field. See @ref dialogueCreation and @ref sequencer for
            /// more information. The special keyword "{{end}}" gets replaced by the default
            /// duration for the subtitle being displayed.
            /// </summary>
            [Tooltip("当对话条目未定义自己的 Sequence 时使用。设置为 Delay({{end}}) 可让 camera 保持不动。")]
            [TextArea]
            public string defaultSequence = "Delay({{end}})";

            /// <summary>
            /// If defined, overrides Default Sequence for player (PC) lines only.
            /// </summary>
            [Tooltip("如果已定义，则仅覆盖玩家（PC）台词的 Default Sequence。")]
            [TextArea]
            public string defaultPlayerSequence = string.Empty;

            /// <summary>
            /// Used when a dialogue entry doesn't define its own Response Menu Sequence.
            /// </summary>
            [Tooltip("当对话条目未定义自己的 Response Menu Sequence 时使用。")]
            [TextArea]
            public string defaultResponseMenuSequence = string.Empty;

            /// <summary>
            /// The format to use for the <c>entrytag</c> keyword.
            /// </summary>
            [Tooltip("用于 'entrytag' 关键字的格式。")]
            public EntrytagFormat entrytagFormat = EntrytagFormat.ActorName_ConversationID_EntryID;

            /// <summary>
            /// Treat all sequencer commands as if they have the 'required' keyword.
            /// </summary>
            [Tooltip("将所有 sequencer 命令都视为带有 'required' 关键字。")]
            public bool treatAllCommandsAsRequired = false;

            /// <summary>
            /// By default, Audio() and AudioWait() sequencer commands don't report 
            /// missing audio files to reduce Console spam during development. Set this
            /// true to report missing audio files.
            /// </summary>
            [Tooltip("默认情况下，Audio() 和 AudioWait() sequencer 命令不会报告缺失的 audio file，以减少开发期间的 Console 噪音。")]
            public bool reportMissingAudioFiles = false;

            /// <summary>
            /// Set <c>true</c> to disable the internal sequencer commands -- for example, if you
            /// want to replace them with your own.
            /// </summary>
            [HideInInspector]
            public bool disableInternalSequencerCommands = false;
        }

        /// <summary>
        /// The camera settings.
        /// </summary>
        public CameraSettings cameraSettings = new CameraSettings();

        [System.Serializable]
        public class InputSettings
        {
            public InputSettings() { }

            public InputSettings(InputSettings source)
            {
                this.alwaysForceResponseMenu = source.alwaysForceResponseMenu;
                this.includeInvalidEntries = source.includeInvalidEntries;
                this.responseTimeout = source.responseTimeout;
                this.responseTimeoutAction = source.responseTimeoutAction;
                this.emTagForOldResponses = source.emTagForOldResponses;
                this.emTagForInvalidResponses = source.emTagForInvalidResponses;
                this.qteButtons = source.qteButtons;
                this.cancel = source.cancel;
                this.cancelConversation = source.cancelConversation;
            }

            /// <summary>
            /// If <c>true</c>, always forces the response menu even if there's only one response.
            /// If <c>false</c>, you can use the <c>[f]</c> tag to force a response.
            /// </summary>
            [Tooltip("即使只有一个 response，也显示 Response Menu。")]
            public bool alwaysForceResponseMenu = true;

            /// <summary>
            /// If `true`, includes responses whose Conditions are false. The `enabled` field of
            /// those responses will be `false`.
            /// </summary>
            [Tooltip("包含 Conditions 为 false 的 response，通常以禁用状态显示。")]
            public bool includeInvalidEntries = false;

            /// <summary>
            /// If not <c>0</c>, the duration in seconds that the player has to choose a response; 
            /// otherwise the currently-focused response is auto-selected. If no response is
            /// focused (e.g., hovered over), the first response is auto-selected. If <c>0</c>,
            /// there is no timeout; the player can take as long as desired to choose a response.
            /// </summary>
            [Tooltip("如果非零，则为 Response Menu 超时前的秒数。")]
            public float responseTimeout = 0f;

            /// <summary>
            /// The response timeout action.
            /// </summary>
            [Tooltip("Response Menu 超时后要执行的操作。")]
            public ResponseTimeoutAction responseTimeoutAction = ResponseTimeoutAction.ChooseFirstResponse;

            /// <summary>
            /// The em tag to wrap around old responses. A response is old if its SimStatus 
            /// is "WasDisplayed". You can change this from EmTag.None if you want to visually
            /// mark old responses in the player response menu.
            /// </summary>
            [Tooltip("用于包裹先前已选择 response 的 [em#] 标签。")]
            public EmTag emTagForOldResponses = EmTag.None;

            /// <summary>
            /// The em tag to wrap around invalid responses. You can change this from EmTag.None 
            /// if you want to visually mark invalid responses in the player response menu.
            /// </summary>
            [Tooltip("用于包裹无效 response 的 [em#] 标签。只有在勾选 Include Invalid Entries 时才会显示这些 response。")]
            public EmTag emTagForInvalidResponses = EmTag.None;

            /// <summary>
            /// The buttons QTE (Quick Time Event) buttons. QTE 0 & 1 default to the buttons
            /// Fire1 and Fire2.
            /// </summary>
            [Tooltip("映射到 QTE 的输入按钮。")]
            public string[] qteButtons = new string[] { "Fire1", "Fire2" };

            /// <summary>
            /// The key and/or button that allows the player to cancel subtitle sequences.
            /// </summary>
            [Tooltip("用于取消 subtitle sequence 的按键或按钮。")]
            public InputTrigger cancel = new InputTrigger(KeyCode.Escape);

            /// <summary>
            /// The key and/or button that allows the player to cancel conversations.
            /// </summary>
            [Tooltip("在 Response Menu 中取消当前对话的按键或按钮。")]
            public InputTrigger cancelConversation = new InputTrigger(KeyCode.Escape);
        }

        /// <summary>
        /// The input settings.
        /// </summary>
        public InputSettings inputSettings = new InputSettings();

        [System.Serializable]
        public class BarkSettings
        {
            public BarkSettings() { }

            public BarkSettings(BarkSettings source)
            {
                this.allowBarksDuringConversations = source.allowBarksDuringConversations;
                this.barkCharsPerSecond = source.barkCharsPerSecond;
                this.minBarkSeconds = source.minBarkSeconds;
                this.defaultBarkSequence = source.defaultBarkSequence;
            }

            /// <summary>
            /// Set <c>true</c> to allow barks to play during conversations.
            /// </summary>
            [Tooltip("允许 barks 在对话期间播放。")]
            public bool allowBarksDuringConversations = true;

            /// <summary>
            /// Show barks for this many characters per second. If zero, use Subtitle Settings > Subtitle Chars Per Second.
            /// </summary>
            [Tooltip("bark 的显示速度（每秒字符数）。如果为 0，则使用 Subtitle Settings > Subtitle Chars Per Second。")]
            public float barkCharsPerSecond = 0;

            /// <summary>
            /// Show barks for at least this many seconds. If zero, use Subtitle Settings > Min Subtitle Seconds.
            /// </summary>
            [Tooltip("bark 至少显示这么多秒。如果为 0，则使用 Subtitle Settings > Min Subtitle Seconds。")]
            public float minBarkSeconds = 0;

            /// <summary>
            /// If non-blank, play this sequence with barks that don't specify their own Sequence.
            /// </summary>
            [Tooltip("如果非空，则对未指定自身 Sequence 的 bark 播放此 Sequence。")]
            public string defaultBarkSequence = string.Empty;

        }

        /// <summary>
        /// The gameplay alert message settings.
        /// </summary>
        public BarkSettings barkSettings = new BarkSettings();

        [System.Serializable]
        public class AlertSettings
        {
            public AlertSettings() { }

            public AlertSettings(AlertSettings source)
            {
                this.allowAlertsDuringConversations = source.allowAlertsDuringConversations;
                this.alertCheckFrequency = source.alertCheckFrequency;
                this.alertCharsPerSecond = source.alertCharsPerSecond;
                this.minAlertSeconds = source.minAlertSeconds;
            }

            /// <summary>
            /// Set <c>true</c> to allow the dialogue UI to show alerts during conversations.
            /// </summary>
            [Tooltip("允许 dialogue UI 在对话期间显示 alerts。")]
            public bool allowAlertsDuringConversations = false;

            /// <summary>
            /// How often to check if the Lua Variable['Alert'] has been set. To disable
            /// automatic monitoring, set this to <c>0</c>.
            /// </summary>
            [Tooltip("如果非零，则按此频率检查 Variable['Alert']，以显示提示消息。")]
            public float alertCheckFrequency = 0f;

            /// <summary>
            /// Show alerts for this many characters per second. If zero, use Subtitle Settings > Subtitle Chars Per Second.
            /// </summary>
            [Tooltip("alert 的显示速度（每秒字符数）。如果为 0，则使用 Subtitle Settings > Subtitle Chars Per Second。")]
            public float alertCharsPerSecond = 0;

            /// <summary>
            /// Show alerts for at least this many seconds. If zero, use Subtitle Settings > Min Subtitle Seconds.
            /// </summary>
            [Tooltip("alert 至少显示这么多秒。如果为 0，则使用 Subtitle Settings > Min Subtitle Seconds。")]
            public float minAlertSeconds = 0;

        }

        /// <summary>
        /// The gameplay alert message settings.
        /// </summary>
        public AlertSettings alertSettings = new AlertSettings();

        public bool ShouldUseOverrides()
        {
            return (conversationOverrideSettings != null) && conversationOverrideSettings.useOverrides;
        }

        public bool ShouldUseSubtitleOverrides()
        {
            return ShouldUseOverrides() && conversationOverrideSettings.overrideSubtitleSettings;
        }

        public bool GetShowNPCSubtitlesDuringLine()
        {
            return ShouldUseSubtitleOverrides() ? conversationOverrideSettings.showNPCSubtitlesDuringLine :
                ((subtitleSettings != null) ? subtitleSettings.showNPCSubtitlesDuringLine : true);
        }

        public bool GetShowNPCSubtitlesWithResponses()
        {
            return ShouldUseSubtitleOverrides() ? conversationOverrideSettings.showNPCSubtitlesWithResponses :
                ((subtitleSettings != null) ? subtitleSettings.showNPCSubtitlesWithResponses : true);
        }

        public bool GetShowPCSubtitlesDuringLine()
        {
            return ShouldUseSubtitleOverrides() ? conversationOverrideSettings.showPCSubtitlesDuringLine :
                ((subtitleSettings != null) ? subtitleSettings.showPCSubtitlesDuringLine : true);
        }

        public bool GetSkipPCSubtitleAfterResponseMenu()
        {
            return ShouldUseSubtitleOverrides() ? conversationOverrideSettings.skipPCSubtitleAfterResponseMenu :
                ((subtitleSettings != null) ? subtitleSettings.skipPCSubtitleAfterResponseMenu : true);
        }

        public float GetSubtitleCharsPerSecond()
        {
            return ShouldUseSubtitleOverrides() ? conversationOverrideSettings.subtitleCharsPerSecond :
                ((subtitleSettings != null) ? subtitleSettings.subtitleCharsPerSecond : 30);
        }


        public float GetMinSubtitleSeconds()
        {
            return ShouldUseSubtitleOverrides() ? conversationOverrideSettings.minSubtitleSeconds :
                ((subtitleSettings != null) ? subtitleSettings.minSubtitleSeconds : 2);
        }

        public SubtitleSettings.ContinueButtonMode GetContinueButtonMode()
        {
            return ShouldUseSubtitleOverrides() ? conversationOverrideSettings.continueButton :
                ((subtitleSettings != null) ? subtitleSettings.continueButton : SubtitleSettings.ContinueButtonMode.Never);
        }

        public bool ShouldUseSequenceOverrides()
        {
            return ShouldUseOverrides() && conversationOverrideSettings.overrideSequenceSettings;
        }

        public string GetDefaultSequence()
        {
            return ShouldUseSequenceOverrides() && !string.IsNullOrEmpty(conversationOverrideSettings.defaultSequence) ? conversationOverrideSettings.defaultSequence :
                ((cameraSettings != null) ? cameraSettings.defaultSequence : string.Empty);
        }

        public string GetDefaultPlayerSequence()
        {
            return ShouldUseSequenceOverrides() && !string.IsNullOrEmpty(conversationOverrideSettings.defaultPlayerSequence) ? conversationOverrideSettings.defaultPlayerSequence :
                ((cameraSettings != null) ? cameraSettings.defaultPlayerSequence : string.Empty);
        }

        public string GetDefaultResponseMenuSequence()
        {
            return ShouldUseSequenceOverrides() && !string.IsNullOrEmpty(conversationOverrideSettings.defaultResponseMenuSequence) ? conversationOverrideSettings.defaultResponseMenuSequence :
                ((cameraSettings != null) ? cameraSettings.defaultResponseMenuSequence : string.Empty);
        }

        public bool ShouldUseInputOverrides()
        {
            return ShouldUseOverrides() && conversationOverrideSettings.overrideInputSettings;
        }

        public bool GetAlwaysForceResponseMenu()
        {
            return ShouldUseInputOverrides() ? conversationOverrideSettings.alwaysForceResponseMenu :
                ((inputSettings != null) ? inputSettings.alwaysForceResponseMenu : true);
        }

        public bool GetIncludeInvalidEntries()
        {
            return ShouldUseInputOverrides() ? conversationOverrideSettings.includeInvalidEntries :
                ((inputSettings != null) ? inputSettings.includeInvalidEntries : true);
        }

        public float GetResponseTimeout()
        {
            return ShouldUseInputOverrides() ? conversationOverrideSettings.responseTimeout :
                ((inputSettings != null) ? inputSettings.responseTimeout : 0);
        }

        public EmTag GetEmTagForOldResponses()
        {
            return ShouldUseInputOverrides() ? conversationOverrideSettings.emTagForOldResponses :
                ((inputSettings != null) ? inputSettings.emTagForOldResponses : EmTag.None);
        }

        public EmTag GetEmTagForInvalidResponses()
        {
            return ShouldUseInputOverrides() ? conversationOverrideSettings.emTagForInvalidResponses :
                ((inputSettings != null) ? inputSettings.emTagForInvalidResponses : EmTag.None);
        }

        public InputTrigger GetCancelSubtitleInput()
        {
            return ShouldUseInputOverrides() ? conversationOverrideSettings.cancelSubtitle :
                ((inputSettings != null) ? inputSettings.cancel : null);
        }

        public InputTrigger GetCancelConversationInput()
        {
            return ShouldUseInputOverrides() ? conversationOverrideSettings.cancelConversation :
                ((inputSettings != null) ? inputSettings.cancelConversation : null);
        }

    }

    /// <summary>
    /// Response timeout action specifies what to do if the response menu times out.
    /// </summary>
    public enum ResponseTimeoutAction
    {
        /// <summary>
        /// Auto-select the first menu choice.
        /// </summary>
        ChooseFirstResponse,

        /// <summary>
        /// Auto-select a random menu choice.
        /// </summary>
        ChooseRandomResponse,

        /// <summary>
        /// End of conversation.
        /// </summary>
        EndConversation,

        /// <summary>
        /// Auto-select current menu choice.
        /// </summary>
        ChooseCurrentResponse,

        /// <summary>
        /// Auto-select the last menu choice.
        /// </summary>
        ChooseLastResponse,

        /// <summary>
        /// Use a custom handler.
        /// </summary>
        Custom
    };

    public enum EmTag
    {
        None,
        Em1,
        Em2,
        Em3,
        Em4
    }

}
