using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug code
/// </summary>
public class ReportAngle : MonoBehaviour
{
    public GameObject centerBody;
    public GameObject body1;
    public GameObject body2;

    private GravityEngine ge;

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 center = centerBody.transform.position;
        double angle = Vector3.Angle(body1.transform.position - center, body2.transform.position - center);
        Debug.LogFormat("angle between {0} and {1} = {2}", body1.gameObject.name, body2.gameObject.name, angle);

    }
}
