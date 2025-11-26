using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MercatorPositionOfNBody : MonoBehaviour
{
    [SerializeField]
    private NBody nbody;

    [SerializeField]
    private MercatorMap map;

    private GravityEngine ge;

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
    }

    // Update is called once per frame
    void Update()
    {
        if (ge.IsSetup()) {
            Vector3 pos = ge.GetPhysicsPosition(nbody);
            transform.position = map.Project(pos, ge.GetPhysicalTime());
        }
    }
}
