using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public static SoundPlayer Instance;

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

        foreach (var entry in soundEntries)
        {
            if (!soundMap.ContainsKey(entry.name))
                soundMap.Add(entry.name, entry.clip);
        }
    }

    public static void PlayClipAtPoint(string clipName, Vector3 position, float volume = 1f)
    {
        if (Instance == null || string.IsNullOrEmpty(clipName) || !Instance.soundMap.TryGetValue(clipName, out var clip))
        {
            return;
        }

        GameObject obj = new GameObject("SFX_" + clipName);
        obj.transform.position = position;

        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;
        source.volume = volume;
        source.Play();

        Destroy(obj, clip.length);
    }
}
