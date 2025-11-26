using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Local Vertical Local Horizontal (LVLH) viewer maps the positions of two NBody
/// objects in orbit around the same center to a co-ordinate frame in which the target
/// object remains fixed. This is a frame that is co-rotating with the target.
///
/// This component is used in conjunction with a camera that shows the motion of these
/// objects in the XY plane.
///
/// The co-ordinate transformation code is handled by a RelativeMotion element that is
/// created once GE startup is complete.
/// 
/// </summary>
public class LVLHViewer : MonoBehaviour
{
    [SerializeField]
    private GameObject targetDot = null;

    [SerializeField]
    private GameObject shipDot = null;

    [SerializeField]
    private NBody target = null;

    [SerializeField]
    private NBody ship = null;

    [SerializeField]
    private NBody planet = null;

    [SerializeField]
    private Vector3 scale = Vector3.one;


    private bool cleared = false;

    private RelativeMotion relMotion; 

    // Start is called before the first frame update
    void Start()
    {
        targetDot.transform.localPosition = Vector3.zero;
        GravityEngine.Instance().AddGEStartCallback(GECallback);
    }

    public void GECallback() {
        relMotion = new RelativeMotion(ship, target, planet);
    }

    // Update is called once per frame
    void Update()
    {
        if (!cleared) {
            // awkward, but clears trail at start
            TrailRenderer tr = shipDot.GetComponentInChildren<TrailRenderer>();
            if (tr.positionCount > 0)
            {
                tr.Clear();
                cleared = true;
            }
        }

        Vector3 localPos = relMotion.ShipToLVLH().ToVector3();
        localPos = Vector3.Scale(localPos, scale);
        shipDot.transform.localPosition = localPos;

    }
}
