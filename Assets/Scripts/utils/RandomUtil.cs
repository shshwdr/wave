using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RandomUtil
{
    public static int RandomBasedOnProbability(List<int> probability)
    {
        int sum = probability.Sum();
        int rand = Random.Range(0, sum);
        for (int i = 0; i < probability.Count; i++)
        {
            rand -= probability[i];
            if (rand <= 0)
            {
                return i;
            }
        }
        Debug.LogError(("RandomUtil.RandomBasedOnProbability: Something went wrong"));
        return 0;
    }
}
