using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AliyunTtsDriver : MonoBehaviour, IJessSpeechDriver
{
    [Header("Aliyun NLS")]
    [SerializeField] private string appKey;
    [SerializeField] private string token;
    private void Awake()
{
    appKey = JessSecrets.Data.aliyunAppKey;
    token = JessSecrets.Data.aliyunToken;
}

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private const string TtsUrl =
        "https://nls-gateway-cn-shanghai.aliyuncs.com/stream/v1/tts";

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("[TTS] Text is empty.");
            return;
        }

        StartCoroutine(SynthesizeAndPlay(text));
    }

    private IEnumerator SynthesizeAndPlay(string text)
    {
        string url =
            TtsUrl +
            "?appkey=" + UnityWebRequest.EscapeURL(appKey) +
            "&token=" + UnityWebRequest.EscapeURL(token) +
            "&text=" + UnityWebRequest.EscapeURL(text) +
            "&format=wav" +
            "&sample_rate=16000";

        Debug.Log("[TTS] Sending request...");

        using (UnityWebRequest request =
               UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "[TTS] Request failed: " +
                    request.responseCode + " / " +
                    request.error
                );

                if (request.downloadHandler != null)
                {
                    Debug.LogError(
                        "[TTS] Server response: " +
                        request.downloadHandler.text
                    );
                }

                yield break;
            }

            AudioClip clip =
                DownloadHandlerAudioClip.GetContent(request);

            if (clip == null)
            {
                Debug.LogError("[TTS] AudioClip is null.");
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();

            Debug.Log("[TTS] Playing.");
        }
    }

    // 先用这个按钮独立测试 TTS。
    [ContextMenu("Test TTS")]
    private void TestTts()
    {
        Speak("你好，我是杰斯。现在我终于可以说话了。");
    }
}
