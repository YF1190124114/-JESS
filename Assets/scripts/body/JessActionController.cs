using UnityEngine;

public class JessActionController : MonoBehaviour
{
    [SerializeField]
    private MonoBehaviour actionDriverComponent;

    private IJessActionDriver actionDriver;

    private void Awake()
    {
        actionDriver =
            actionDriverComponent as IJessActionDriver;

        if (actionDriver == null)
        {
            Debug.LogWarning(
                "[JESS Action Controller] Action Driver 还没有正确连接"
            );
        }
    }

    public void PlayAction(string action)
    {
        Debug.Log(
            "[JESS Body] 收到 action = " + action
        );

        if (actionDriver == null)
        {
            Debug.LogWarning(
                "[JESS Body] 当前没有 Action Driver"
            );
            return;
        }

        if (action == "idle")
        {
            actionDriver.StopAction();
            return;
        }

        actionDriver.PlayAction(action);
    }
}