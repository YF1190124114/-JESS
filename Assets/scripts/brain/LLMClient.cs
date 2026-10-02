using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMClient : MonoBehaviour
{
    [Header("LLM Config")]
    [SerializeField] private string apiKey = "";
    private void Awake()
{
    apiKey = JessSecrets.Data.qwenApiKey;
}
    
    // 先填你百炼控制台对应地域的完整 chat/completions 地址
    [SerializeField] private string endpoint = "";

    [SerializeField] private string model = "qwen-plus";

    [Serializable]
    private class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public Message[] messages;
    }

    [Serializable]
    private class ResponseMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class Choice
    {
        public ResponseMessage message;
    }

    [Serializable]
    private class ChatResponse
    {
        public Choice[] choices;
    }

    // 外部以后只需要调用这个
    public void Ask(
        string systemPrompt,
        string userText,
        Action<string> onSuccess,
        Action<string> onError = null)
    {
        StartCoroutine(
            SendRequest(systemPrompt, userText, onSuccess, onError)
        );
    }

    private IEnumerator SendRequest(
        string systemPrompt,
        string userText,
        Action<string> onSuccess,
        Action<string> onError)
    {
        Message[] messages =
        {
            new Message
            {
                role = "system",
                content = systemPrompt
            },
            new Message
            {
                role = "user",
                content = userText
            }
        };

        ChatRequest requestData = new ChatRequest
        {
            model = model,
            messages = messages
        };

        string json = JsonUtility.ToJson(requestData);

        using UnityWebRequest request =
            new UnityWebRequest(endpoint, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + apiKey
        );

        Debug.Log("[LLM] Sending request...");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error =
                $"HTTP {request.responseCode}\n" +
                request.error + "\n" +
                request.downloadHandler.text;

            Debug.LogError("[LLM] " + error);
            onError?.Invoke(error);
            yield break;
        }

        string responseJson = request.downloadHandler.text;

        Debug.Log("[LLM RAW] " + responseJson);

        ChatResponse response =
            JsonUtility.FromJson<ChatResponse>(responseJson);

        if (response == null ||
            response.choices == null ||
            response.choices.Length == 0 ||
            response.choices[0].message == null)
        {
            string error = "LLM 返回格式无法解析。";
            Debug.LogError(error);
            onError?.Invoke(error);
            yield break;
        }

        string answer = response.choices[0].message.content;

        Debug.Log("[LLM ANSWER] " + answer);

        onSuccess?.Invoke(answer);
    }
}