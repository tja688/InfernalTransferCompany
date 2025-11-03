using UnityEngine;

/// <summary>
/// 【终极版】UnityEvent 音频播放中转站
/// 
/// 这个脚本是一个“中转”或“包装器”，专门用于让 Unity Events 
/// (如 UI 按钮点击、动画事件等) 能够方便地播放
/// Audio Manager Pro 的所有音频类型。
/// 
/// 它提供了返回值为 void 的公共方法，以便在 Unity Event 列表中显示，
/// 并且允许你直接拖拽 ScriptableObject 资产。
/// 
/// **使用方法：**
/// 1. 把这个脚本挂载到你场景中存放 Audio Manager 的游戏对象上。
/// 2. 在 Unity Event (例如 Button.OnClick) 中：
/// 3. 拖拽挂载了此脚本的游戏对象到事件栏的 Object 字段。
/// 4. 在 Function 下拉菜单中，选择 "UnityEventAudioPlayer" -> 对应的播放方法。
/// 5. 把你的 SFXObject, SFXGroup, Track, 或 Playlist 资产文件拖到新出现的参数字段中。
/// </summary>
public class UnityEventAudioPlayer : MonoBehaviour
{
    // --- 1. SFX (音效) 播放 ---

    /// <summary>
    /// (用于 Unity Event) 播放一个 SFXObject (单个音效)
    /// </summary>
    public void Play(SFXObject sfx)
    {
        if (sfx != null)
        {
            // 调用 SFXManager 的 Play，并忽略返回的 Coroutine
            SFXManager.Main.Play(sfx);
        }
    }

    /// <summary>
    /// (用于 Unity Event) 播放一个 SFXGroup (随机音效组)
    /// </summary>
    public void Play(SFXGroup sfxGroup)
    {
        if (sfxGroup != null)
        {
            SFXManager.Main.Play(sfxGroup);
        }
    }

    /// <summary>
    /// (用于 Unity Event) 通过名字从 Library 播放 SFXObject
    /// </summary>
    public void PlaySFXObjectFromLibrary(string sfxName)
    {
        if (!string.IsNullOrEmpty(sfxName))
        {
            SFXManager.Main.PlayFromSFXObjectLibrary(sfxName);
        }
    }

    /// <summary>
    /// (用于 Unity Event) 通过名字从 Library 播放 SFXGroup
    /// </summary>
    public void PlaySFXGroupFromLibrary(string sfxGroupName)
    {
        if (!string.IsNullOrEmpty(sfxGroupName))
        {
            SFXManager.Main.PlayFromSFXGroupLibrary(sfxGroupName);
        }
    }

    // --- 2. Music (音乐) 播放 ---

    /// <summary>
    /// (用于 Unity Event) 播放一个 Track (单首音乐)
    /// </summary>
    public void Play(Track musicTrack)
    {
        if (musicTrack != null)
        {
            // MusicManager.Play(Track) 本身就是 void，
            // 但我们在这里封装一层，以保持所有事件调用都在这个脚本上，更整洁。
            MusicManager.Main.Play(musicTrack);
        }
    }

    /// <summary>
    /// (用于 Unity Event) 通过名字从 Library 播放 Track
    /// </summary>
    public void PlayTrackFromLibrary(string trackName)
    {
        if (!string.IsNullOrEmpty(trackName))
        {
            MusicManager.Main.PlayFromLibrary(trackName);
        }
    }

    // --- 3. Playlist (播放列表) 播放 ---

    /// <summary>
    /// (用于 Unity Event) 播放一个 Playlist (音乐播放列表)
    /// </summary>
    public void Play(Playlist playlist)
    {
        if (playlist != null)
        {
            // PlaylistManager.Play(Playlist, float) 也是 void，
            // 我们调用它的基础版本。
            PlaylistManager.Main.Play(playlist);
        }
    }

    /// <summary>
    /// (用于 Unity Event) 通过名字从 Library 播放 Playlist
    /// </summary>
    public void PlayPlaylistFromLibrary(string playlistName)
    {
        if (!string.IsNullOrEmpty(playlistName))
        {
            PlaylistManager.Main.PlayFromLibrary(playlistName);
        }
    }

    // --- 4. 通用控制 (停止) ---

    /// <summary>
    /// (用于 Unity Event) 停止当前播放的音乐 (Track)
    /// </summary>
    public void StopMusic()
    {
        // MusicManager.Stop() 是 void
        MusicManager.Main.Stop(); 
    }

    /// <summary>
    /// (用于 Unity Event) 停止所有 SFX 音效
    /// </summary>
    public void StopAllSFX()
    {
        // SFXManager.StopAll() 是 void
        SFXManager.Main.StopAll();
    }

    /// <summary>
    /// (用于 Unity Event) 停止当前的 Playlist
    /// </summary>
    public void StopPlaylist()
    {
        // PlaylistManager.Stop() 是 void
        PlaylistManager.Main.Stop();
    }
}