using UnityEngine;

/// <summary>
/// Simple runtime utility that plays a random clip from a manually assigned Audio Manager Pro SFXGroup
/// whenever the user presses the P key.
/// </summary>
public class AudioManagerProClipTester : MonoBehaviour
{
    [Header("Audio Manager Pro")]
    [Tooltip("Clip group created by Audio Manager Pro. A random clip will be played from this group when P is pressed.")]
    [SerializeField] private SFXGroup clipGroup;

    [Tooltip("Keyboard key used to trigger playback. Defaults to 'P'.")]
    [SerializeField] private KeyCode triggerKey = KeyCode.P;

    private void Awake()
    {
        if (clipGroup == null)
        {
            Debug.LogWarning($"{nameof(AudioManagerProClipTester)} on '{name}' is missing a clip group reference.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            if (clipGroup == null)
            {
                Debug.LogWarning($"Cannot play clip group because none is assigned on {nameof(AudioManagerProClipTester)} attached to '{name}'.");
                return;
            }

            if (SFXManager.Main == null)
            {
                Debug.LogWarning("No active Audio Manager Pro SFXManager was found in the scene.");
                return;
            }

            SFXManager.Main.Play(clipGroup);
        }
    }
}
