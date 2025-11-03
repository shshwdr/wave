
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtension
{

    public static Transform  GetPreviousSibling(this Transform current)
    {
        var parent = current.parent;
        if (parent == null) return null;

        int index = current.GetSiblingIndex();
        if (index == 0) return null; // 已经是第一个

        return parent.GetChild(index - 1);
    }
}
