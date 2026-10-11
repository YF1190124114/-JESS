using UnityEngine;

public class JessExpressionDemo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer face;
    [SerializeField] private JessSpeechController speechController;

    [Header("Expression")]
    [SerializeField] private float transitionDuration = 0.5f;

    private readonly string[] expressions =
    {
        "Fcl_ALL_Neutral",
        "Fcl_ALL_nailong",
        "Fcl_ALL_tiaomei",
        "Fcl_ALL_buxie",
        "Fcl_ALL_jiaxiao"
    };

    private readonly string[] lines =
    {
        "真是光阴似箭啊……",
        "过得跟屎一样。",
        "身边还都是贱人。",
        "光想着阴我了……",
        "还没似成。"
    };

    private readonly float[] timeline =
    {
        0f,
        2.0f,
        3.8f,
        5.8f,
        7.8f
    };

    private const string FullText =
        "真是光阴似箭啊，过得跟屎一样，身边还都是贱人，光想着阴我了，还没似成。";

    private int[] indices;
    private float[] currentWeights;
    private float[] startWeights;
    private float[] targetWeights;

    private int currentExpression = 0;
    private int nextCue = 1;

    private float elapsed = 0f;
    private float transitionElapsed = 0f;
    private bool transitioning = false;

    void Start()
    {
        if (face == null)
        {
            Debug.LogError("[JESS 小剧场] Face 没连接");
            enabled = false;
            return;
        }

        indices = new int[expressions.Length];
        currentWeights = new float[expressions.Length];
        startWeights = new float[expressions.Length];
        targetWeights = new float[expressions.Length];

        for (int i = 0; i < expressions.Length; i++)
        {
            indices[i] =
                face.sharedMesh.GetBlendShapeIndex(expressions[i]);

            Debug.Log(
                "[JESS 小剧场] " +
                expressions[i] +
                " index = " +
                indices[i]
            );
        }

        // 初始 Neutral
        currentWeights[0] = 100f;
        targetWeights[0] = 100f;

        Debug.Log("[JESS 小剧场] 开始演出");

        if (speechController != null)
        {
            speechController.Speak(FullText);
        }
        else
        {
            Debug.LogWarning(
                "[JESS 小剧场] Speech Controller 没连接，只演表情"
            );
        }

        ShowCue(0, false);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        if (nextCue < timeline.Length &&
            elapsed >= timeline[nextCue])
        {
            ShowCue(nextCue, true);
            nextCue++;
        }

        UpdateTransition();
    }

    void ShowCue(int cue, bool smooth)
    {
        currentExpression = cue;

        Debug.Log(
            "[JESS 小剧场] " +
            lines[cue] +
            "  →  " +
            expressions[cue]
        );

        if (!smooth)
            return;

        for (int i = 0; i < currentWeights.Length; i++)
        {
            startWeights[i] = currentWeights[i];
            targetWeights[i] = 0f;
        }

        targetWeights[cue] = 100f;

        transitionElapsed = 0f;
        transitioning = true;
    }

    void UpdateTransition()
    {
        if (!transitioning)
            return;

        transitionElapsed += Time.deltaTime;

        float t;

        if (transitionDuration <= 0f)
            t = 1f;
        else
            t = Mathf.Clamp01(
                transitionElapsed / transitionDuration
            );

        // SmoothStep：比普通线性插值柔和
        t = t * t * (3f - 2f * t);

        for (int i = 0; i < currentWeights.Length; i++)
        {
            currentWeights[i] =
                Mathf.Lerp(
                    startWeights[i],
                    targetWeights[i],
                    t
                );
        }

        if (transitionElapsed >= transitionDuration)
        {
            for (int i = 0; i < currentWeights.Length; i++)
                currentWeights[i] = targetWeights[i];

            transitioning = false;
        }
    }

    void LateUpdate()
    {
        if (face == null || indices == null)
            return;

        // 必须在 LateUpdate 最终落地，
        // 防止目前未知的外部系统覆盖 BlendShape。
        for (int i = 0; i < indices.Length; i++)
        {
            if (indices[i] >= 0)
            {
                face.SetBlendShapeWeight(
                    indices[i],
                    currentWeights[i]
                );
            }
        }
    }
}
