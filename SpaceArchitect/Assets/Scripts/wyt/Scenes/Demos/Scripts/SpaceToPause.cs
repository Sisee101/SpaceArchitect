using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceToPause : MonoBehaviour
{

    [SerializeField]
    private bool pauseAfterInit = false;

    void Start()
    {
        GravityEngine.Instance().AddGEStartCallback(GEStart); 
    }

    private void GEStart()
    {
        GravityEngine.Instance().SetEvolve(!pauseAfterInit);
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GravityEngine.Instance().SetEvolve(!GravityEngine.Instance().GetEvolve());
    }
}
