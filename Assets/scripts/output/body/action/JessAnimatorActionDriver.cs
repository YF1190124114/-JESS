using UnityEngine;

public class JessAnimatorActionDriver : MonoBehaviour, IJessActionDriver
{
    [SerializeField]
    private Animator animator;

    public void PlayAction(string actionName)
    {
        if (animator == null)
        {
            Debug.LogWarning(
                "[JESS Animator Driver] Animator 没有连接"
            );
            return;
        }

        Debug.Log(
            "[JESS Animator Driver] 执行动作 = " + actionName
        );

        animator.CrossFade(actionName, 0.2f);
    }

    public void StopAction()
    {
        if (animator == null)
        {
            Debug.LogWarning(
                "[JESS Animator Driver] Animator 没有连接"
            );
            return;
        }

        Debug.Log(
            "[JESS Animator Driver] 回到 idle"
        );

        animator.CrossFade("idle", 0.2f);
    }

    // ===== 临时测试 =====
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[JESS TEST] 按下 T，测试 testaction");
            PlayAction("testaction");
        }
    }
}