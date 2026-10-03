using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Aliyun NLS")]
    public string appKey;
    public string token;
    private void Awake()
{
    appKey = JessSecrets.Data.aliyunAppKey;
    token = JessSecrets.Data.aliyunToken;
}

    private AudioClip recordedClip;
    private string microphoneDevice;

    private const int SampleRate = 48000;

  IEnumerator Start()
{
    Debug.Log("🎤 正在检查 macOS 麦克风权限...");

    if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
    {
        Debug.Log("⚠️ 当前没有麦克风权限，正在请求...");

        yield return Application.RequestUserAuthorization(
            UserAuthorization.Microphone
        );
    }

    if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
    {
        Debug.LogError("❌ macOS 没有授予 Unity 麦克风权限！");
        yield break;
    }

    Debug.Log("✅ macOS 已授予 Unity 麦克风权限！");

    if (Microphone.devices.Length == 0)
    {
        Debug.LogError("❌ Unity 没有检测到麦克风！");
        yield break;
    }

    Debug.Log("====== Unity检测到的所有麦克风 ======");

    for (int i = 0; i < Microphone.devices.Length; i++)
    {
        Debug.Log("🎙️ [" + i + "] " + Microphone.devices[i]);
    }

    Debug.Log("================================");

    microphoneDevice = Microphone.devices[0];
    int minFreq;
int maxFreq;

Microphone.GetDeviceCaps(microphoneDevice, out minFreq, out maxFreq);

Debug.Log("🎚️ 麦克风最低支持采样率：" + minFreq);
Debug.Log("🎚️ 麦克风最高支持采样率：" + maxFreq);

    Debug.Log("🎤 当前使用：" + microphoneDevice);
}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Microphone.IsRecording(microphoneDevice))
                StopRecording();
            else
                StartRecording();
        }
    }

    void StartRecording()
    {
        Debug.Log("🔴 开始录音...");

        recordedClip = Microphone.Start(
            microphoneDevice,
            false,
            30,
            SampleRate
        );
    }

    void StopRecording()
{
    int samplePosition = Microphone.GetPosition(microphoneDevice);

    Microphone.End(microphoneDevice);

    // ===== 诊断信息 =====
    Debug.Log("🎧 AudioClip 实际采样率：" + recordedClip.frequency);
    Debug.Log("🎧 AudioClip 实际声道数：" + recordedClip.channels);
    Debug.Log("🎧 Microphone Position：" + samplePosition);
    Debug.Log("🎧 AudioClip 总采样帧数：" + recordedClip.samples);

    if (samplePosition <= 0)
        {
            Debug.LogError("❌ 没有录到声音");
            return;
        }

        Debug.Log(
            "⏹️ 录音结束：" +
            ((float)samplePosition / SampleRate).ToString("F2") +
            " 秒"
        );

        byte[] pcmData = AudioClipToPCM16(recordedClip, samplePosition);

        Debug.Log("📦 PCM 数据大小：" + pcmData.Length + " bytes");
        Debug.Log("☁️ 正在发送给阿里云识别...");

        StartCoroutine(SendToAliyun(pcmData));
    }

    byte[] AudioClipToPCM16(AudioClip clip, int sampleCount)
    {
        int channels = clip.channels;

       float[] samples = new float[sampleCount * channels];

bool getDataSuccess = clip.GetData(samples, 0);

Debug.Log("🧪 GetData读取成功：" + getDataSuccess);
Debug.Log("🧪 AudioClip loadState：" + clip.loadState);

        // 如果麦克风是双声道，转换成单声道
        float[] monoSamples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float sum = 0f;

            for (int channel = 0; channel < channels; channel++)
            {
                sum += samples[i * channels + channel];
            }

            monoSamples[i] = sum / channels;
        }
        // ===== 检查录进去的声音到底有没有振幅 =====
float maxAmplitude = 0f;
float sumAmplitude = 0f;

for (int i = 0; i < monoSamples.Length; i++)
{
    float abs = Mathf.Abs(monoSamples[i]);

    if (abs > maxAmplitude)
        maxAmplitude = abs;

    sumAmplitude += abs;
}

float averageAmplitude = sumAmplitude / monoSamples.Length;

Debug.Log("🔊 最大音量：" + maxAmplitude);
Debug.Log("🔊 平均音量：" + averageAmplitude);

        byte[] pcm = new byte[monoSamples.Length * 2];

        for (int i = 0; i < monoSamples.Length; i++)
        {
            float sample = Mathf.Clamp(monoSamples[i], -1f, 1f);

            short value = (short)(sample * short.MaxValue);

            pcm[i * 2] = (byte)(value & 0xff);
            pcm[i * 2 + 1] = (byte)((value >> 8) & 0xff);
        }

        return pcm;
    }

    IEnumerator SendToAliyun(byte[] pcmData)
    {
        string url =
            "https://nls-gateway-cn-shanghai.aliyuncs.com/stream/v1/asr" +
            "?appkey=" + UnityWebRequest.EscapeURL(appKey) +
            "&format=pcm" +
            "&sample_rate=16000" +
            "&enable_punctuation_prediction=true" +
            "&enable_inverse_text_normalization=true";

        UnityWebRequest request =
            new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

        request.uploadHandler = new UploadHandlerRaw(pcmData);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/octet-stream"
        );

        request.SetRequestHeader(
            "X-NLS-Token",
            token
        );

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ 阿里云返回：");
            Debug.Log(request.downloadHandler.text);

            AliyunResult result =
                JsonUtility.FromJson<AliyunResult>(
                    request.downloadHandler.text
                );

            if (result != null && !string.IsNullOrEmpty(result.result))
            {
                Debug.Log("🗣️ 你刚才说的是：" + result.result);
            }
            else
            {
                Debug.LogWarning("⚠️ 请求成功，但没有识别到文字");
            }
        }
        else
        {
            Debug.LogError("❌ 阿里云请求失败");
            Debug.LogError("HTTP: " + request.responseCode);
            Debug.LogError(request.error);
            Debug.LogError(request.downloadHandler.text);
        }

        request.Dispose();
    }

    [Serializable]
    public class AliyunResult
    {
        public string task_id;
        public string result;
        public int status;
        public string message;
    }
}