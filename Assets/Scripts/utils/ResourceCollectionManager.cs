using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public enum ResourceType
{
    gold, sapphire,coin,gem,generalFragment,featureUnlock,bpExp,mineral
}
public struct  ResourceFlyObjData
{
    public Vector2 start;
    public Transform target;
    public ResourceType resourceType;
    public int amount;
    public bool screenTrans;
    public string subType;
    public float scale;
    
}
public class ResourceCollectionManager : Singleton<ResourceCollectionManager>
{
    public GameObject collectionPrefab;
    public GameObject collectionTextPrefab;
    IObjectPool<GameObject> resourceCollectionPool;
    // Collection checks will throw errors if we try to release an item that is already in the pool.
    public bool collectionChecks = true;
    public int maxPoolSize = 10;
    public Transform parent;

    List<ResourceFlyObjData> resourceFlyObjDatas = new List<ResourceFlyObjData>();
    // public void ShowResourceCollection(Transform trans, ResourceType resourceType, int amount,bool screenTrans = false,string subType = null)
    // {
    //     //ResourceTargetType targetType = Enum.Parse<ResourceTargetType>(resourceType.ToString());
    //     ShowResourceCollection(trans, resourceType, amount,screenTrans,subType);
    // }
    
    public void ShowResourceCollection(Vector2 start, Transform target, ResourceType resourceType,int amount,string resourceSubType = null,bool screenTrans = false,float scale = 1)
    {
        if (resourceCollectionPool == null)
        {
            return;
        }

        var resourceFlyObjData = new ResourceFlyObjData()
        {
            start = start,
            resourceType = resourceType,
            amount = amount,
            screenTrans = screenTrans,
             target = target
             ,subType = resourceSubType,
             scale = scale,
        };
        resourceFlyObjDatas.Add(resourceFlyObjData);
    }

    private float flyTime = 0.1f;
    private float flyTimer = 0;
    
    private void Update()
    {
        flyTimer+= Time.deltaTime;
        if (resourceFlyObjDatas.Count > 0 && flyTimer>flyTime)
        {
            flyTimer = 0;
            var resourceFlyObjData = resourceFlyObjDatas[0];

            var screenPosition = resourceFlyObjData.start;
            // if (!resourceFlyObjData.screenTrans)
            // {
            //     screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, resourceFlyObjData.start);
            // }
            
            //先随机显示1-20个
            int flyAmount = resourceFlyObjData.amount;
           // var resourceInfo = CSVLoader.Instance.getResourceInfo(resourceFlyObjData.resourceType.ToString());
            // if (resourceInfo!=null)
            // {
            //     flyAmount /= resourceInfo.flyDivident;
            // }
            //flyAmount = Random.Range(4, 10);
            flyAmount = math.clamp(flyAmount, 1, 20);
            for (int i = 0; i < flyAmount; i++)
            {
                var go = resourceCollectionPool.Get();
                go.transform.position = screenPosition;
                //var obj = Instantiate(collectionPrefab, go.transform);
                go.GetComponent<ResourceCollectionFlyingObject>().Init(resourceFlyObjData, 0.05f*i,screenPosition,resourceFlyObjData.target.position);
            }
            
            //go.GetComponent<ResourceCollectionFlyingObject>().Init(resourceFlyObjData.resourceType,resourceFlyObjData.resourceTargetType, resourceFlyObjData. amount);
            resourceFlyObjDatas.RemoveAt(0);
        }
            
            
    }

    public GameObject FetchCollectionItem()
    {
        var go  = resourceCollectionPool.Get();
        return go;
    }
    

    // Start is called before the first frame update
    void Start()
    {
        resourceCollectionPool = new ObjectPool<GameObject>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionChecks, 10, maxPoolSize);
    }

    GameObject CreatePooledItem()
    {
        GameObject go = Instantiate(collectionPrefab,parent);
        return go;
    }
   
    // Called when an item is taken from the pool using Get
    void OnTakeFromPool(GameObject go)
    {
        go.gameObject.SetActive(true);
        go.GetComponent<PoolObject>().Init(resourceCollectionPool);
    }
// Called when an item is returned to the pool using Release
    void OnReturnedToPool(GameObject system)
    {
        system.gameObject.SetActive(false);
    }
    // If the pool capacity is reached then any items returned will be destroyed.
    // We can control what the destroy behavior does, here we destroy the GameObject.
    void OnDestroyPoolObject(GameObject system)
    {
        Destroy(system.gameObject);
    }
}
