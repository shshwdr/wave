using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地图曲线连线，沿二次贝塞尔采样绘制贴图条带。
/// </summary>
public class MapCurvedLine : Image
{
    [SerializeField] private int segmentCount = 20;

    private Vector2 pointA;
    private Vector2 pointB;
    private Vector2 controlPoint;
    private float thickness = 10f;
    private bool hasGeometry;

    public void Setup(Vector2 from, Vector2 to, float perpendicularOffset, float lineThickness)
    {
        pointA = from;
        pointB = to;
        thickness = Mathf.Max(1f, lineThickness);

        Vector2 mid = (from + to) * 0.5f;
        Vector2 dir = to - from;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
            controlPoint = mid + perp * perpendicularOffset;
            hasGeometry = true;
        }
        else
        {
            controlPoint = mid;
            hasGeometry = false;
        }

        UpdateRectBounds();
        SetVerticesDirty();
    }

    private void UpdateRectBounds()
    {
        if (!hasGeometry)
        {
            return;
        }

        int segs = Mathf.Max(4, segmentCount);
        Vector2 min = pointA;
        Vector2 max = pointA;
        float halfThick = thickness * 0.5f;

        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            Vector2 p = EvaluateBezier(t);
            Vector2 tangent = EvaluateBezierTangent(t);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = pointB - pointA;
            }

            tangent.Normalize();
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            Vector2 left = p + normal * halfThick;
            Vector2 right = p - normal * halfThick;

            min = Vector2.Min(min, Vector2.Min(left, right));
            max = Vector2.Max(max, Vector2.Max(left, right));
        }

        RectTransform rt = rectTransform;
        rt.anchoredPosition = (min + max) * 0.5f;
        rt.sizeDelta = max - min;
        rt.localRotation = Quaternion.identity;
    }

    private Vector2 EvaluateBezier(float t)
    {
        float u = 1f - t;
        return u * u * pointA + 2f * u * t * controlPoint + t * t * pointB;
    }

    private Vector2 EvaluateBezierTangent(float t)
    {
        float u = 1f - t;
        return 2f * u * (controlPoint - pointA) + 2f * t * (pointB - controlPoint);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (!hasGeometry || sprite == null)
        {
            return;
        }

        int segs = Mathf.Max(4, segmentCount);
        float halfThick = thickness * 0.5f;
        Vector2 origin = rectTransform.anchoredPosition;

        Vector2[] leftVerts = new Vector2[segs + 1];
        Vector2[] rightVerts = new Vector2[segs + 1];
        float[] cumLen = new float[segs + 1];
        cumLen[0] = 0f;

        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            Vector2 center = EvaluateBezier(t) - origin;
            Vector2 tangent = EvaluateBezierTangent(t);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = pointB - pointA;
            }

            tangent.Normalize();
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            leftVerts[i] = center + normal * halfThick;
            rightVerts[i] = center - normal * halfThick;

            if (i > 0)
            {
                cumLen[i] = cumLen[i - 1] + Vector2.Distance(leftVerts[i], leftVerts[i - 1]);
            }
        }

        float totalLen = Mathf.Max(cumLen[segs], 0.001f);
        Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
        Color32 color32 = color;

        for (int i = 0; i < segs; i++)
        {
            float u0 = cumLen[i] / totalLen;
            float u1 = cumLen[i + 1] / totalLen;
            int baseIndex = vh.currentVertCount;

            vh.AddVert(leftVerts[i], color32, new Vector2(Mathf.Lerp(uv.x, uv.z, u0), uv.w));
            vh.AddVert(rightVerts[i], color32, new Vector2(Mathf.Lerp(uv.x, uv.z, u0), uv.y));
            vh.AddVert(leftVerts[i + 1], color32, new Vector2(Mathf.Lerp(uv.x, uv.z, u1), uv.w));
            vh.AddVert(rightVerts[i + 1], color32, new Vector2(Mathf.Lerp(uv.x, uv.z, u1), uv.y));

            vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            vh.AddTriangle(baseIndex + 1, baseIndex + 3, baseIndex + 2);
        }
    }
}
