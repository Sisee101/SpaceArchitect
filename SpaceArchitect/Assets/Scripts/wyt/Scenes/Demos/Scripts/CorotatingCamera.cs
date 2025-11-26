using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Control a camera so that it shows the system co-rotating with a specific 
/// planet in orbit around a star. This can be useful for showing free-return 
/// paths to a moon as a figure eight or to show the interesting orbits that 
/// occur when a ship orbits a Lagrange point in a suitable system. (Note that for
/// the Lagrange points L4/L5 to be stable Mplanet <= 0.03852 Mstar).
/// 
/// The script takes a reference to a planet in orbit around a star and assumes the 
/// planet has an OrbitUniversal component.
/// 
/// This script is typically used for circular orbits but will work for elliptical orbits 
/// as well.
///
/// The script supports multiple camera anchors, allowing the camera to move to a view with the
/// center body selected to views around various Lagrange points. 
/// 
/// </summary>
[RequireComponent(typeof(Camera))]
public class CorotatingCamera : MonoBehaviour
{
	//! Camera will match the rotation of this planet so it appears to be stationary.
    [SerializeField]
	private NBody planet = null;

    [SerializeField]
    [Tooltip("Camera anchors (GameObjects) to be selected (slot 0 =F1, slot 1=F2 etc.)\nScene starts with 0 selected. ")]
    private GameObject[] anchors = null;

    private int selectedAnchor = 0;

    //! Camera to rotate
    private Camera corotatingCamera = null;

    private NBody centerBody;

    private Vector3 cameraOffset;

	private Vector3[] initialCameraPos; 

    private Vector3 rotationAxis;
    private float omega;
    private float time0;

    private Quaternion initialRotation; 

    private GravityEngine ge; 

    private void Start()
    {
        ge = GravityEngine.Instance();
        // need to init camera after any attached Lagrange points have inited
        corotatingCamera = GetComponent<Camera>();
        cameraOffset = corotatingCamera.transform.position;
        SetAnchor(0);
        ge.AddGEStartCallback(GEStart, 100);
    }

    private void GEStart()
    {
        OrbitUniversal planetOrbit = planet.GetComponent<OrbitUniversal>();
        rotationAxis = planetOrbit.GetAxis().ToVector3();
        omega = (float)planetOrbit.GetAngularVelocity();
        centerBody = planetOrbit.centerNbody;
        Vector3 cmWorld = ge.MapPhyPosToWorld(OrbitUtils.CenterOfMass(planet, centerBody).ToVector3());

        initialRotation = corotatingCamera.transform.rotation;
        initialCameraPos = new Vector3[anchors.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            initialCameraPos[i] = anchors[i].transform.position - cmWorld;
        }

        time0 = ge.GetPhysicalTime();
        Debug.LogFormat("{0}: pos={1}", gameObject.name, initialCameraPos );

    }

    void FixedUpdate()
    {
    	if (!ge.IsSetup()) {
             return;
    	}

        Vector3 cmWorld = ge.MapPhyPosToWorld(OrbitUtils.CenterOfMass(planet, centerBody).ToVector3());
        float angleDeg = (ge.GetPhysicalTime() - time0) * omega * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angleDeg, rotationAxis);
        Vector3 cameraPos = rotation * initialCameraPos[selectedAnchor] + cmWorld + cameraOffset;

        corotatingCamera.transform.position = cameraPos;
        corotatingCamera.transform.rotation = rotation * initialRotation;

    }

    private void SetAnchor(int a)
    {
        selectedAnchor = a;
        corotatingCamera.transform.parent = anchors[selectedAnchor].transform;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < anchors.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.F1 + i))
            {
                SetAnchor(i);
                break;
            }
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            selectedAnchor = (selectedAnchor+1) % anchors.Length;
            Debug.LogFormat("V " + selectedAnchor);
            SetAnchor(selectedAnchor);
        }
    }


}
