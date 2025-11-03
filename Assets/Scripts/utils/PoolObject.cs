using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolObject : MonoBehaviour
{
    public float duration = 3;
    private IObjectPool<GameObject> pool;

    private bool isReleased = false;
    // Start is called before the first frame update
    public void Init(IObjectPool<GameObject> pool)
    {
        this.pool = pool;
        StartCoroutine(OnParticleSystemStopped());
    }

    private void OnEnable()
    {
        isReleased = false;
    }

    IEnumerator OnParticleSystemStopped()
    {
        if (!isReleased)
        {
            yield return new WaitForSeconds(duration);
            // Return to the pool
            pool.Release(gameObject);
            isReleased = true;
        }
    }

    public void  OnStop()
    {
        if (!isReleased)
        {
            pool.Release(gameObject);
            isReleased = true;
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
