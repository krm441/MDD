using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SoundPlayer : MonoBehaviour
{
    public static SoundPlayer Instance;
    [SerializeField] private AudioSource musicSource; // ambient
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup ambientGroup;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxGroup;


    [System.Serializable]
    public class SoundEntry
    {
        public string name;
        public AudioClip clip;
    }

    [SerializeField] private List<SoundEntry> soundEntries = new List<SoundEntry>();
    private Dictionary<string, AudioClip> soundMap = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Assign ambient mixer group
        if (musicSource != null && ambientGroup != null)
            musicSource.outputAudioMixerGroup = ambientGroup;

        foreach (var entry in soundEntries)
        {
            if (!soundMap.ContainsKey(entry.name))
                soundMap.Add(entry.name, entry.clip);
        }
    }

    public static void PlayClipAtPoint(string clipName, Vector3 position, float volume = 1f, bool duckMusic = true)
    {
        if (Instance == null || string.IsNullOrEmpty(clipName) || !Instance.soundMap.TryGetValue(clipName, out var clip))
        {
            return;
        }

        GameObject obj = new GameObject("SFX_" + clipName);
        //obj.transform.position = position;
        //obj.transform.position = Camera.main.transform.position;

        // Position the sound a short distance in front of the camera:
        // this will keep the 3d effect, and not muffle the sound completely.
        Vector3 camPos = Camera.main.transform.position;
        Vector3 camForward = Camera.main.transform.forward;
        obj.transform.position = camPos + camForward * 4.5f; // 4.5 meters in front = good position till now

        AudioSource source = obj.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = Instance?.sfxGroup;
        source.clip = clip;
        source.spatialBlend = 1f;// 0f;
        source.volume = volume;
        source.Play();

        Destroy(obj, clip.length);

        if (duckMusic)
            Instance.DuckMusic(0.2f, 0.5f); // ← duck music temporarily
    }

    public static void PlayMusic(string clipName, float volume = 1f)
    {
        if (Instance == null || string.IsNullOrEmpty(clipName) || !Instance.soundMap.TryGetValue(clipName, out var clip))
        {
            return;
        }

        Instance.musicSource.clip = clip;
        Instance.musicSource.volume = volume;
        Instance.musicSource.Play();
    }


    // ========================= DUCK ======================== //
    private Coroutine duckCoroutine;
    public void DuckMusic(float duckVolume = 0.2f, float duration = 0.5f)
    {
        if (duckCoroutine != null)
            StopCoroutine(duckCoroutine);

        duckCoroutine = StartCoroutine(DuckRoutine(duckVolume, duration));
    }

    private IEnumerator DuckRoutine(float duckVolume, float duration)
    {
        float originalVolume = musicSource.volume;

        musicSource.volume = duckVolume;
        yield return new WaitForSeconds(duration);
        musicSource.volume = originalVolume;
    }

}
