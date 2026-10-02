using UnityEngine;
using System;
using System.Collections.Generic;

public class JessBlendShapeExpressionDriver : MonoBehaviour, IJessExpressionDriver
{
    [Serializable]
    public class BlendShapeTarget
    {
        public SkinnedMeshRenderer mesh;
        public string blendShapeName;

        [Range(0f, 1f)]
        public float targetWeight = 1f;
    }

    [Serializable]
    public class ExpressionMapping
    {
        public string expressionName;
        public List<BlendShapeTarget> targets = new List<BlendShapeTarget>();
    }

    [SerializeField]
    private List<ExpressionMapping> expressions =
        new List<ExpressionMapping>();


    public void SetExpression(string expressionName, float weight)
    {
        ExpressionMapping mapping =
            expressions.Find(x => x.expressionName == expressionName);

        if (mapping == null)
        {
            Debug.LogWarning(
                "[JESS BlendShape Driver] 未找到 expression = " +
                expressionName
            );
            return;
        }

        foreach (BlendShapeTarget target in mapping.targets)
        {
            if (target.mesh == null)
                continue;

            int index =
                target.mesh.sharedMesh.GetBlendShapeIndex(
                    target.blendShapeName
                );

            if (index < 0)
            {
                Debug.LogWarning(
                    "[JESS BlendShape Driver] 找不到 BlendShape = " +
                    target.blendShapeName
                );
                continue;
            }

            float finalWeight =
                weight * target.targetWeight * 100f;

            target.mesh.SetBlendShapeWeight(
                index,
                finalWeight
            );
        }
    }


    public void ClearExpressions()
    {
        foreach (ExpressionMapping mapping in expressions)
        {
            foreach (BlendShapeTarget target in mapping.targets)
            {
                if (target.mesh == null)
                    continue;

                int index =
                    target.mesh.sharedMesh.GetBlendShapeIndex(
                        target.blendShapeName
                    );

                if (index >= 0)
                {
                    target.mesh.SetBlendShapeWeight(
                        index,
                        0f
                    );
                }
            }
        }
    }
}