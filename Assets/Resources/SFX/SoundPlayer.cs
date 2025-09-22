using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SoundPlayer : MonoBehaviour 
{
    AudioSource audioSource;

    [System.Serializable]
    public class SoundEntry
    {
        public string name;
        public AudioClip clip;
    }

    [SerializeField] private List<SoundEntry> soundEntries = new List<SoundEntry>();
    private Dictionary<string, AudioClip> soundMap = new Dictionary<string, AudioClip>();

    bool initialized = false; // lazy init

    public bool muteMusic = false;

    private void Init()
    {
        if (initialized) return;    // lazy init
        foreach (var entry in soundEntries)
        {
            if (!soundMap.ContainsKey(entry.name))
            {
                soundMap.Add(entry.name, entry.clip);
            }
        }
        initialized = true;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        Init();
        PlayMusic(soundEntries[0].name);
    }

    public void PlayClipAtPoint(string clipName, Vector3 position, float volume = 1f, bool duckMusic = true)
    {
        Init();
        if (!soundMap.TryGetValue(clipName, out var clip))
        {
            Console.Error("Music: ", clipName, "not found");
            return;
        }

        GameObject obj = new GameObject("SFX_" + clipName);
        Vector3 camPos = Camera.main.transform.position;
        Vector3 camForward = Camera.main.transform.forward;
        obj.transform.position = camPos + camForward * 4.5f; // 4.5 meters in front = good position till now

        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;
        source.volume = volume;
        source.Play();

        Destroy(obj, clip.length);

        if (duckMusic)
            DuckMusic(0.2f, 0.5f);
    }

    public void PlayMusic(string clipName, float volume = 1f)
    {
        if (muteMusic) return;

        Init();
        if (!soundMap.TryGetValue(clipName, out var clip))
        {
            Console.Error("Music: ", clipName, "not found");
            return;
        }

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }


    // ========================= DUCK ======================== //
    private Coroutine duckCoroutine;
    public void DuckMusic(float duckVolume = 0.2f, float duration = 0.5f)
    {
        if (duckCoroutine != null)
            StopCoroutine(duckCoroutine);

        //duckCoroutine = StartCoroutine(DuckRoutine(duckVolume, duration));
    }

    //private IEnumerator DuckRoutine(float duckVolume, float duration)
    //{
    //    float originalVolume = musicSource.volume;
    //
    //    musicSource.volume = duckVolume;
    //    yield return new WaitForSeconds(duration);
    //    musicSource.volume = originalVolume;
    //}

}
