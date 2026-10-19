using UnityEngine;
using System.Collections;

public class JessExpressionController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour expressionDriverComponent;
    [SerializeField] private float transitionDuration = 0.6f;

    private IJessExpressionDriver expressionDriver;
    private Coroutine transitionCoroutine;

    private string currentExpression = "";
    private float currentWeight = 0f;

    private void Awake()
    {
        expressionDriver =
            expressionDriverComponent as IJessExpressionDriver;

        if (expressionDriver == null)
        {
            Debug.LogError(
                "[JESS Expression Controller] Expression Driver 没有正确连接"
            );
        }
    }

    public void SetExpression(string expression)
    {
        Debug.Log("[JESS Body] 收到 expression = " + expression);

        if (expressionDriver == null)
        {
            Debug.LogError("[JESS Body] 没有 Expression Driver");
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine =
            StartCoroutine(TransitionToExpression(expression));
    }

    private IEnumerator TransitionToExpression(string targetExpression)
    {
        string startExpression = currentExpression;
        float startWeight = currentWeight;

        float time = 0f;

        while (time < transitionDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / transitionDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            // 旧表情逐渐消失
            if (!string.IsNullOrEmpty(startExpression))
            {
                float oldWeight =
                    Mathf.Lerp(startWeight, 0f, t);

                expressionDriver.SetExpression(
                    startExpression,
                    oldWeight
                );
            }

            // 新表情逐渐出现
            float newWeight = Mathf.Lerp(0f, 1f, t);

            expressionDriver.SetExpression(
                targetExpression,
                newWeight
            );

            yield return null;
        }

        // 最终清干净，再只保留目标表情
        expressionDriver.ClearExpressions();

        expressionDriver.SetExpression(
            targetExpression,
            1f
        );

        currentExpression = targetExpression;
        currentWeight = 1f;

        Debug.Log(
            "[JESS Body] transition completed = " +
            targetExpression
        );

        transitionCoroutine = null;
    }
}