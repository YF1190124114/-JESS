using UnityEngine;
using UniVRM10;

public class JessVRMExpressionDriver : MonoBehaviour, IJessExpressionDriver
{
    [SerializeField] private Vrm10Instance vrmInstance;

    public void SetExpression(string expressionName, float weight)
    {
        ExpressionKey key;

        switch (expressionName)
        {
            case "neutral":
                key = ExpressionKey.Neutral;
                break;

            case "angry":
                key = ExpressionKey.Angry;
                break;

            case "happy":
                key = ExpressionKey.Happy;
                break;

            case "sad":
                key = ExpressionKey.Sad;
                break;

            case "surprised":
                key = ExpressionKey.Surprised;
                break;

            case "fun":
                key = ExpressionKey.Relaxed;
                break;

            default:
                Debug.LogWarning(
                    "[JESS VRM Driver] 未知 expression = " + expressionName
                );
                return;
        }

        vrmInstance.Runtime.Expression.SetWeight(key, weight);
    }

    public void ClearExpressions()
    {
        vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Neutral, 0f);
        vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Angry, 0f);
        vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Happy, 0f);
        vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Sad, 0f);
        vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Surprised, 0f);
        vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Relaxed, 0f);
    }
}