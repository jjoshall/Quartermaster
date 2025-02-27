using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public static class AudioLibrary {
    // Asynchronously load an AudioClip via Addressables.
    public static void GetClipAsync(string audioKey, Action<AudioClip> callback) {
        Addressables.LoadAssetAsync<AudioClip>(audioKey).Completed += (AsyncOperationHandle<AudioClip> handle) => {
            if (handle.Status == AsyncOperationStatus.Succeeded) {
                callback(handle.Result);
            } else {
                Debug.LogError("Failed to load AudioClip from Addressables: " + audioKey);
                callback(null);
            }
        };
    }
}
