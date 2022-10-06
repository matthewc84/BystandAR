using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        this.transform.SetParent(GameObject.Find("ParentAnchor").transform);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
