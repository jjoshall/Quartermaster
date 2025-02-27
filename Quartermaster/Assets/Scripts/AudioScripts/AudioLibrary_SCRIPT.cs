using UnityEngine;
using System.Collections.Generic;

public static class AudioLibrary {
    private static Dictionary<string, AudioClip> audioClips;

    public static void Initialize() {
        audioClips = new Dictionary<string, AudioClip>();
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        foreach (AudioClip clip in clips) {
            audioClips[clip.name] = clip;
        }
    }

    public static AudioClip GetClip(string clipName) {
        if (audioClips == null) {
            Initialize();
        }
        audioClips.TryGetValue(clipName, out AudioClip clip);
        return clip;
    }
}
