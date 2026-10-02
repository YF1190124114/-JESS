public interface IJessExpressionDriver
{
    // 让模型执行某个 JESS 表情
    void SetExpression(string expressionName, float weight);

    // 清除当前模型上的所有 JESS 表情
    void ClearExpressions();
}