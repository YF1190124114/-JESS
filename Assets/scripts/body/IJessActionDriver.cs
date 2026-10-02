public interface IJessActionDriver
{
    // 让当前模型执行一个 JESS 语义动作
    void PlayAction(string actionName);

    // 回到默认待机状态
    void StopAction();
}