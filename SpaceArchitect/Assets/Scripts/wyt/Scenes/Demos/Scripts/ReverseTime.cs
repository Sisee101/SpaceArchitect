using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReverseTime : MonoBehaviour
{
    private bool reversed = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            reversed = !reversed;
            GravityEngine.Instance().SetTimeReversed(reversed);
            if (!GravityEngine.Instance().GetEvolve() && !reversed)
                GravityEngine.Instance().SetEvolve(true);
        }
    }
}
