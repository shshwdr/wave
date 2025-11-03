using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class MyLogger
{
    [Conditional("ENABLE_DEBUG_LOG")]
    public static void Log(string content)
    {
        Debug.Log(content);
    }
    
}
