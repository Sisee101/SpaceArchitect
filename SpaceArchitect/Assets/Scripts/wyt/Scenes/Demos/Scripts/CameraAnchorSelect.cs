using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script to toggle a camera parent between a number of anchor objects.
///
/// Keys F1..FN select a specific camera anchor
///      V step through the list of anchors
/// </summary>
public class CameraAnchorSelect : MonoBehaviour {

    [SerializeField]
    [Tooltip("Cameras (GameObjects) to be selected (slot 0 =F1, slot 1=F2 etc.)\nScene starts with 0 selected. ")]
    private GameObject[] anchors = null;

    [SerializeField]
    private Camera myCamera = null;

    private int selectedAnchor;

    private Vector3 relativePos; 

    // Use this for initialization
    void Start () {
        selectedAnchor = 0;
        myCamera.transform.parent = anchors[selectedAnchor].transform;
        relativePos = myCamera.transform.localPosition;
	}
	
	// Update is called once per frame
	void Update () {
		for (int i=0; i < anchors.Length; i++) {
            if (Input.GetKeyDown(KeyCode.F1 + i)) {
                selectedAnchor = i;
                myCamera.transform.parent = anchors[selectedAnchor].transform;
                myCamera.transform.position = relativePos + anchors[selectedAnchor].transform.position;
                break;
            }
        }
        if (Input.GetKeyDown(KeyCode.V)){
            selectedAnchor = (selectedAnchor++) % anchors.Length;
            myCamera.transform.parent = anchors[selectedAnchor].transform;
            myCamera.transform.localPosition = relativePos;
        }
    }
}
