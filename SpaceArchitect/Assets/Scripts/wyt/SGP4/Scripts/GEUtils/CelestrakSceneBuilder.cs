using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This game object is not used at run time but is an editor tool to do a bulk add of Earth satellites from a file or 
/// text area of data from the Celestrak data base. 
/// 
/// It requires that the scene be in ORBITAL units. 
/// 
/// Each satellite will be created from the provided prefab. The prefab is required to have an NBody and OrbitUniversal. 
/// 
/// The satellites will be optionally made children of the object designated as parent. If not provided they will be top level 
/// objects in the scene. 
/// 
/// The tag field will be added to all created prefabs. This simplifies finding them in cases where they need to be deleted. 
/// 
/// </summary>
public class CelestrakSceneBuilder : MonoBehaviour
{
    public GameObject satellitePrefab;

    public NBody centerBody; 

    public string fileName; 

    public GameObject parent;

    public string lastMessage;

    /// <summary>
    /// Given the information in the file specified and under command of the editor button "Create"
    /// - read the file as triplets of satellite info
    /// - create an object from the prefab, make child of parent if specified
    /// - set it to be inited from TLE data
    /// - paste in the 3 lines of TLE data and init the orbit in the scene
    /// </summary>
    public string CreateSatellites()
    {
        if (fileName.EndsWith(".txt")) {
            return "Do not include .txt extension in name";
        }

        TextAsset mytxtData = (TextAsset)Resources.Load(fileName);
        if (mytxtData == null) {
            return "Could not access file: " + fileName;
        }
        string txt = mytxtData.text;

        string[] lines = txt.Split('\n');
        if (lines.Length % 3 != 0) {
            Debug.LogError("Number of lines in satellite data must be multiple of 3. Have " + lines.Length );
        }
        Debug.Log("File ok");

        // Loop through the satellite lines
        for (int s=0; s < lines.Length/3; s++) {
            GameObject go = Instantiate<GameObject>(satellitePrefab);
            go.name = lines[s * 3];
            if (parent != null) {
                go.transform.parent = parent.transform;
            }
            OrbitUniversal ou = go.GetComponent<OrbitUniversal>();
            ou.inputMode = OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET;
            ou.tleName = lines[s * 3];
            ou.tleLine1 = lines[s * 3 + 1];
            ou.tleLine2 = lines[s * 3 + 2];
            ou.centerNbody = centerBody;
            ou.InitCOEFromTLEData();
        }

        return null; 
    }


}
