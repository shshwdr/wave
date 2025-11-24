using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetTransparent : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        GetComponent<Image>().color = new Color(1,1,1,0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
