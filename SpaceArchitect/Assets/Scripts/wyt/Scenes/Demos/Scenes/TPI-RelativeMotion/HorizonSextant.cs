using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple UI script to show a sextant from local horizonal to the specified angle.
///
/// Place as a child on the ship that is doing the measurement. Typically used to visualize
/// the trigger angle to a target. 
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class HorizonSextant : MonoBehaviour
{
    [SerializeField]
    private float radius = 1f;

    [SerializeField]
    private float angle = 10f;

	[SerializeField]
    private NBody ship = null;

    [SerializeField]
    private NBody planet = null;

    private LineRenderer lineR;

    private GravityEngine ge; 

    // Start is called before the first frame update
    void Start()
    {
        lineR = GetComponent<LineRenderer>();
        ge = GravityEngine.Instance();
    }

	public void SetAngle(float angle)
	{
        this.angle = angle;
	}

    // Update is called once per frame
    void Update()
    {
        Vector3 shipPos = ge.GetPhysicsPosition(ship) - ge.GetPhysicsPosition(planet);
        Vector3 shipVel = ge.GetVelocity(ship) - ge.GetVelocity(planet);
        Vector3 orbitAxis = Vector3.Cross(shipVel, shipPos).normalized;
        Vector3 circDir = Vector3.Cross(orbitAxis, shipPos).normalized;
        Vector3 horizon = Vector3.Project(shipVel, circDir).normalized;

        Vector3[] points = new Vector3[3];
        points[0] = ge.MapToScene(shipPos + radius * horizon);
        points[1] = ge.MapToScene(shipPos);
        Vector3 sextantPoint = Quaternion.AngleAxis(angle, orbitAxis) * horizon;
        points[2] = ge.MapToScene(shipPos + radius * sextantPoint);
        lineR.SetPositions(points);
        lineR.positionCount = 3;

    }
}
