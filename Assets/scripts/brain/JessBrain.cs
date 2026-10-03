using System;
using UnityEngine;

public class JessBrain : MonoBehaviour
{
    [SerializeField] private LLMClient llmClient;
    [SerializeField] private JessExpressionController expressionController;
    [SerializeField] private JessActionController actionController;
    [SerializeField] private JessSpeechController speechController;

    [Serializable]
    public class BrainResult
    {
        public string reply;
        public string expression;
        public string action;
    }

    private void Start()
    {
        TestBrain();
    }

    private void TestBrain()
    {
        string systemPrompt =
            "你叫JESS，是一个自然、有个性的数字人。" +
            "你必须只返回JSON，不要返回Markdown，不要使用```代码块，也不要添加任何解释。" +
            "JSON格式必须严格为：" +
            "{\"reply\":\"你要说的话\",\"expression\":\"表情\",\"action\":\"动作\"}" +
            "expression只能选择以下之一：" +
            "neutral, angry, fun, happy, sad, surprised。" +
            "action目前只能填写idle。" +
            "reply使用简短、自然的中文。";

        string testInput = "你居然把我最喜欢的杯子摔了而且一句道歉都没有!";

        Debug.Log("[JESS Brain] User: " + testInput);

        llmClient.Ask(
            systemPrompt,
            testInput,

            rawAnswer =>
            {
                Debug.Log("[JESS Brain RAW] " + rawAnswer);

                try
                {
                    BrainResult result =
                        JsonUtility.FromJson<BrainResult>(rawAnswer);

                    if (result == null ||
                        string.IsNullOrEmpty(result.reply) ||
                        string.IsNullOrEmpty(result.expression) ||
                        string.IsNullOrEmpty(result.action))
                    {
                        Debug.LogError("[JESS Brain] JSON字段缺失");
                        return;
                    }

                    Debug.Log(
                        "[JESS Brain RESULT]\n" +
                        "reply = " + result.reply + "\n" +
                        "expression = " + result.expression + "\n" +
                        "action = " + result.action
                    );

                    // Brain → Expression Controller
                    expressionController.SetExpression(result.expression);

                    // Brain → Action Controller
                    actionController.PlayAction(result.action);

                    // Brain → Speech
                    speechController.Speak(result.reply);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        "[JESS Brain] JSON解析失败: " + e.Message
                    );
                }
            },

            error =>
            {
                Debug.LogError("[JESS Brain API Error] " + error);
            }
        );
    }
}