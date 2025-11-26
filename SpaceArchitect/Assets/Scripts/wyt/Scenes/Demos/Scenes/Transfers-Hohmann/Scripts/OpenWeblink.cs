using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple UI element
/// </summary>
public class OpenWeblink : MonoBehaviour {

    public string url;

	// Use this for initialization
	void Start () {
		
	}
	
	public void OpenLink() {
        Debug.Log("Open " + url);
        Application.OpenURL(url);
    }
}
