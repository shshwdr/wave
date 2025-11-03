using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Utils : MonoBehaviour
{
    static public float gridSize = 0.32f;
    static public Vector2[] dir4V2 = { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(0, 1), };
    static public Vector2Int[] dir4V2Int = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1), };
    static public Vector2[] dir5V2 = { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, 0), };
    public static float localDistanceUnder(Transform a, Transform b, Transform commonParent)
    {
        Vector3 aLocalInCommon = commonParent.InverseTransformPoint(a.position);
        Vector3 bLocalInCommon = commonParent.InverseTransformPoint(b.position);
        return Vector2.Distance((Vector2)aLocalInCommon, (Vector2)bLocalInCommon);
    }
    
    public static float localVerticleDistanceUnder(Transform a, Transform b, Transform commonParent)
    {
        Vector3 aLocalInCommon = commonParent.InverseTransformPoint(a.position);
        Vector3 bLocalInCommon = commonParent.InverseTransformPoint(b.position);
        return math.abs(aLocalInCommon.y - bLocalInCommon.y);
    }
    public static float localHorizontalDistanceUnder(Transform a, Transform b, Transform commonParent)
    {
        Vector3 aLocalInCommon = commonParent.InverseTransformPoint(a.position);
        Vector3 bLocalInCommon = commonParent.InverseTransformPoint(b.position);
        return math.abs(aLocalInCommon.x - bLocalInCommon.x);
    }
    static public Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
    {
        Vector3 line_direction = line_end - line_start;
        float line_length = line_direction.magnitude;
        line_direction.Normalize();
        float project_length = Mathf.Clamp(Vector3.Dot(point - line_start, line_direction), 0f, line_length);
        return line_start + line_direction * project_length;
    }
    // For infinite lines:
    Vector3 GetClosestPointOnInfiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
    {
        return line_start + Vector3.Project(point - line_start, line_end - line_start);
    }

    static public T randomList<T>(List<T> a)
    {
        if(a.Count == 0)
        {
            return default(T);
        }
        return a[Random.Range(0, a.Count)];
    }

    static public bool ListEqual<T>(List<T> a, List<T> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (!a[i].Equals( b[i]))
                return false;
        }
        return true;
    }

    static public Vector3 roundTo90(Vector3 input)
    {
        return new Vector3(roundTo90(input.x), roundTo90(input.y), roundTo90(input.z));
    }
    static public  float roundTo90(float input)
    {
        return Mathf.Round(input / 90f) * 90;
    }
    T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        System.Type type = original.GetType();
        var dst = destination.GetComponent(type) as T;
        if (!dst) dst = destination.AddComponent(type) as T;
        var fields = type.GetFields();
        foreach (var field in fields)
        {
            if (field.IsStatic) continue;
            field.SetValue(dst, field.GetValue(original));
        }
        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
            prop.SetValue(dst, prop.GetValue(original, null), null);
        }
        return dst as T;
    }

    static public bool randomBool()
    {
        return Random.Range(0, 2) > 0;
    }
    static public int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    static public float distanceWithoutY(Vector3 p,Vector3 t)
    {
        Vector3 diff = p - t;
        return new Vector3(diff.x, 0, diff.z).magnitude;
    }
    static public bool arrayContains<T>(T[] array, T target)
    {
        foreach (T t in array)
        {
            if (target.Equals(t))
            {
                return true;
            }
        }
        return false;
    }

    static public bool colliderContainFromTop(Collider collider, Collider innerCollider)
    {
        Vector3 minBound = new Vector3(innerCollider.bounds.min.x, collider.bounds.min.y, innerCollider.bounds.min.z);
        Vector3 maxBound = new Vector3(innerCollider.bounds.max.x, collider.bounds.min.y, innerCollider.bounds.max.z);

        return collider.bounds.Contains(minBound) && collider.bounds.Contains(maxBound);
    }

    static public List<T> findAround<T>(Vector3 position, float radius, List<T> candicateTransforms) where T : MonoBehaviour
    {
        var res = new List<T>();
        foreach(var t in candicateTransforms)
        {
            if ((t.transform.position - position).magnitude < radius)
            {
                res.Add(t);
            }
        }
        return res;
    }
    
    static public int findClosestIndex<T>(Vector3 position, T[] candicateTransforms) where T : MonoBehaviour
    {
        int res = 0;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < candicateTransforms.Length; i++)
        {
            if (!candicateTransforms[i])
            {
                continue;
            }
            float distance = (candicateTransforms[i].transform.position - position).magnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                res = i;
            }
        }
        return res;
    }

    static public T findClosestItem<T>(Vector3 targetPosition, List<T> candicateTransforms) where T : MonoBehaviour
    {
        return findClosestItem(targetPosition, candicateTransforms.ToArray());
    }
    static public T findClosestItem<T>(Vector3 targetPosition, T[] candicateTransforms) where T : MonoBehaviour
    {
        int res = 0;
        if(candicateTransforms.Length == 0)
        {
            return null;
        }
        float closestDistance = float.MaxValue;
        for (int i = 0; i < candicateTransforms.Length; i++)
        {
            if (!candicateTransforms[i])
            {
                continue;
            }
            float distance = (candicateTransforms[i].transform.position - targetPosition).magnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                res = i;
            }
        }
        return candicateTransforms[res];
    }


    static public T findClosestTransform<T>(Vector3 targetPosition, T[] candicateTransforms) where T : Transform
    {
        int res = 0;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < candicateTransforms.Length; i++)
        {
            if (!candicateTransforms[i])
            {
                continue;
            }
            float distance = (candicateTransforms[i].transform.position - targetPosition).magnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                res = i;
            }
        }
        return candicateTransforms[res];
    }

    static public Vector3 randomVector3(Vector3 origin, float randomness)
    {
        return new Vector3(origin.x + randomFromZero(randomness), origin.y + randomFromZero(randomness), origin.z + randomFromZero(randomness));
    }

    static public Vector3 randomVector3_2d(Vector3 origin, float randomness)
    {
        return new Vector3(origin.x + randomFromZero(randomness), origin.y + randomFromZero(randomness), origin.z);
    }

    static public float randomFromZero(float randomness)
    {
        return Random.Range(-randomness, randomness);
    }

    public static T randomEnumValue<T>()
    {
        var values = System.Enum.GetValues(typeof(T));
        int random = UnityEngine.Random.Range(0, values.Length);
        return (T)values.GetValue(random);
    }

    public static int enumLength<T>()
    {
        var values = System.Enum.GetValues(typeof(T));
        return values.Length;
    }

    static float snapFloat(float origin)
    {
        return Mathf.Round(origin / gridSize) * gridSize;
    }

    static float snapFloatCenter( float origin)
    {
        return Mathf.Round((origin - gridSize / 2f) / gridSize) * gridSize + gridSize / 2f;
    }

    static float floatToGridIndexCenter(float origin)
    {
        return Mathf.RoundToInt((origin - gridSize / 2f) / gridSize);
    }

    static int floatToGridIndex(float origin)
    {
        return Mathf.RoundToInt(origin / gridSize);
    }

    public static Vector3 snapToGrid(Vector3 origin)
    {
        return new Vector3(snapFloat(origin.x), snapFloat(origin.y), snapFloat(origin.z));
    }

    public static int distanceToIndex(float distance)
    {
        return Mathf.RoundToInt(distance / gridSize);
    }
    public static int distanceCenterToIndex(float distance)
    {
        return Mathf.RoundToInt((distance - gridSize / 2f) / gridSize);
    }

    public static Vector3 snapToGridCenter(Vector3 origin)
    {
        return new Vector3(snapFloatCenter(origin.x), snapFloatCenter(origin.y), snapFloatCenter(origin.z));
    }

    public static Vector3 snapToGridBottom(Vector3 origin)
    {
        return new Vector3(snapFloatCenter(origin.x), snapFloat(origin.y), snapFloatCenter(origin.z));
    }

    public static Vector2 positionToGridIndexCenter2d(Vector3 origin)
    {
        return new Vector2(floatToGridIndexCenter(origin.x), floatToGridIndexCenter(origin.y));
    }

    public static Vector2Int positionToGridIndex2d(Vector3 origin)
    {
        return new Vector2Int(floatToGridIndex(origin.x), floatToGridIndex(origin.y));
    }


    public static List<int> randomMultipleIndex(int count, int selectCount)
    {
  //      for i := 1 to k
  //    R[i] := S[i]

  //// replace elements with gradually decreasing probability
  //          for i := k + 1 to n
  //  (*randomInteger(a, b) generates a uniform integer from the inclusive range { a, ..., b}
  //      *)
  //  j:= randomInteger(1, i)
  //  if j <= k
  //      R[j] := S[i]
        List<int> res = new List<int>();
        if(count == 0)
        {
            return res;
        }
        for (int i = 0; i < selectCount; i++)
        {
            res.Add(i);

        }
        for(int i = selectCount; i < count; i++)
        {
            int j = Random.Range(0, i);
            if (j < selectCount)
            {
                res[j] = i;
            }
        }


        return res;
    }
    public static bool nextToPositionInGrid(Vector3 p1, Vector3 p2)
    {
        var positionIndex1 = positionToGridIndexCenter2d(p1);
        var positionIndex2 = positionToGridIndexCenter2d(p2);
        if ((positionIndex1 - positionIndex2).magnitude <= 1.1f)
        {
            return true;
        }
        return false;
    }

    public static Vector2 chaseDir2d(Vector3 chaser, Vector3 chasee)
    {
        var diff = chasee - chaser;
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return new Vector2(diff.x > 0 ? 1 : -1, 0);
        }
        else
        {

            return new Vector2(0, diff.y > 0 ? 1 : -1);
        }
    }

    public static Vector2 chaseDir2dSecond(Vector3 chaser, Vector3 chasee)
    {
        var diff = chasee - chaser;
        if (Mathf.Abs(diff.x) <= Mathf.Abs(diff.y))
        {
            return new Vector2(diff.x > 0 ? 1 : -1, 0);
        }
        else
        {

            return new Vector2(0, diff.y > 0 ? 1 : -1);
        }
    }

    public static string GetNextIntWithString(string identifier)
    {
        var shortIdentifier = identifier.Substring(0, identifier.Length - 1);
        var value =int.Parse( identifier.Substring(identifier.Length - 1));
        value++;
        var nextIdentifier = shortIdentifier + value;
        return nextIdentifier;
    }
    public static string GetNextIntWithString(string identifier,int v)
    {
        var shortIdentifier = identifier.Substring(0, identifier.Length - 1);
        var value =int.Parse( identifier.Substring(identifier.Length - 1));
        value+=v;
        var nextIdentifier = shortIdentifier + value;
        return nextIdentifier;
    }

    public static Color GetColorFromString(string hexColor)
    {
        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
           // MyLogger.Log("Parsed color: " + color);
            // 这里你可以将color应用到物体的颜色等
            return  color;
        }
        else
        {
            Debug.LogWarning("Invalid color string.");
            return Color.white;
            
        }
    }
    public static List<T> SubtractLists<T>(List<T> list1, List<T> list2)
    {
        List<T> result = new List<T>(list1);
        foreach (T item in list2)
        {
            result.Remove(item); // 每次只移除一个匹配项
        }
        return result;
    }
    public static int GetRandomIndex(List<float> weights)
    {
        if (weights == null || weights.Count == 0)
            throw new ArgumentException("Weights list cannot be null or empty.");

        // 计算权重总和
        float totalWeight = 0f;
        foreach (var weight in weights)
        {
            if (weight < 0)
                throw new ArgumentException("Weights cannot be negative.");
            totalWeight += weight;
        }

        if (Mathf.Approximately(totalWeight, 0))
            throw new ArgumentException("Total weight cannot be zero.");

        // 生成随机数
        float randomValue = UnityEngine.Random.Range(0, totalWeight);

        // 遍历权重数组，找到随机值所属的区间
        float cumulativeWeight = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue < cumulativeWeight)
            {
                return i;
            }
        }

        // 理论上不会到达这里
        return -1;
    }


    static public void destroyAllChildren(Transform tran)
    {
        foreach (Transform child in tran)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    static public void setActiveOfAllChildren(Transform tran, bool active = false)
    {
        foreach (Transform child in tran)
        {
            child.gameObject.SetActive(active);
        }
    }

    static public int[] arrayAggregasion(int[] a, int[] b, int multipler = 1)
    {
        if (a.Length == 0)
        {
            return b;
        }
        if (a.Length != b.Length)
        {
            Debug.LogError("can't solve this!");
            return a;
        }
        for (int i = 0; i < a.Length; i++)
        {
            a[i] += b[i] * multipler;
        }
        return a;
    }
}
