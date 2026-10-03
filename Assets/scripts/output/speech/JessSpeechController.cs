using UnityEngine;

public class JessSpeechController : MonoBehaviour
{
    [SerializeField] private AliyunTtsDriver speechDriver;

    public void Speak(string text)
    {
        if (speechDriver == null)
        {
            Debug.LogError("[JESS Speech] Speech Driver 未设置");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("[JESS Speech] Speak 收到空文本");
            return;
        }

        speechDriver.Speak(text);
    }
}