using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceCollectionFlyingObject : MonoBehaviour
{
    public TMP_Text lebel;
    public Image icon;
    private float startOffset = 0;
    private Vector3 controlPoint;
    Vector3 endPosition;
    private Vector3 startPosition;
    private ResourceFlyObjData resourceData;

    public int currencySize = 75;
    public int otherSize = 112;

    public void SetSprite(Sprite sp)
    {
         icon.sprite = sp;
    }
     public void Init(ResourceFlyObjData resourceData,float startOffset,Vector3 startPosition,Vector3 endPosition,bool startMove = true)
     {
         this.resourceData = resourceData;
         // if (amount >= 0)
         // {
         //     //set TMP_Text color
         //     lebel.color = Color.black;
         // }
         // else
         // {
         //     lebel.color = Color.red;
         // }
         //
         // var amountString = StringUtils.FormatNumber(amount);
         // lebel.text = amount>0? "+"+amountString: amountString;
         this.startOffset = startOffset;
         this.startPosition = startPosition;
         this.endPosition = endPosition;
         controlPoint = GetRandomControlPoint(startPosition, endPosition);
         var resourceType = resourceData.resourceType;
         icon.transform.localScale = Vector3.one;
         if (resourceData.resourceType == ResourceType.gold || resourceData.resourceType == ResourceType.sapphire)
         {
             var type = resourceData.resourceType == ResourceType.gold ? "GoldC" : "SapphireC";
             var iconSprite = type+Random.Range(1,4);
             icon.sprite = Resources.Load<Sprite>("ore/"+iconSprite);
         }
          icon.transform.localScale *= Random.Range(0.8f, 1f);
          icon.transform.localScale *= resourceData.scale;
         // if (resourceType == ResourceType.Minion || resourceType == ResourceType.Equipment ||
         //     resourceType == ResourceType.Skill)
         // {
         //     icon.rectTransform.sizeDelta = new Vector2(otherSize, otherSize);
         // }
         // else
         // {
         //     icon.rectTransform.sizeDelta = new Vector2(currencySize, currencySize);
         // }
         //icon.SetNativeSize();
         // transform.DOMove(ResourceManager.Instance.GetResourcePosition(targetType),
         //     GetComponent<PoolObject>().duration);
         if (startMove)
         {
             StartCoroutine(MoveResource(gameObject, startPosition, endPosition, controlPoint, startOffset, 1.0f,resourceData));
         }
     }

     public void StartMove()
     {
         
         StartCoroutine(MoveResource(gameObject, startPosition, endPosition, controlPoint, startOffset, 1.0f,resourceData));
     }
     
     Vector3 GetRandomControlPoint(Vector3 start, Vector3 end)
     {
         // 生成一个在起点和终点之间的随机控制点，生成弧线
         float midX = (start.x + end.x) / 2 + Random.Range(-0.1f, 0.1f);
         float midY = Mathf.Max(start.y, end.y) /*+ Random.Range(-100.0f, 100.0f)*/; // 控制弧线高度
         float midZ = (start.z + end.z) / 2;
         return new Vector3(midX, midY, midZ);
     }
     Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
     {
         // 二阶贝塞尔曲线公式
         float u = 1 - t;
         float tt = t * t;
         float uu = u * u;
         Vector3 p = uu * p0; // (1-t)^2 * p0
         p += 2 * u * t * p1; // 2 * (1-t) * t * p1
         p += tt * p2; // t^2 * p2
         return p;
     }
     
     IEnumerator MoveResource(GameObject resource, Vector3 start, Vector3 end, Vector3 control,float yieldTime, float duration,ResourceFlyObjData resourceData)
     {
         yield return new WaitForSeconds(yieldTime);
         float time = 0;
         while (time < duration)
         {
             time += Time.deltaTime;
             float t = time / duration;
             Vector3 position = CalculateBezierPoint(t, start, control, end);
             resource.transform.position = position;
             yield return null;
         }

         

         if (GetComponent<PoolObject>())
         {
             
             //SfxManager.Instance.PlayGetResourceSfx();
             GetComponent<PoolObject>().OnStop();
         }
     }
}
