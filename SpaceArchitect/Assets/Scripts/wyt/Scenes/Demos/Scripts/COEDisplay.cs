using UnityEngine;
using UnityEngine.UI;

public class COEDisplay : MonoBehaviour
{
    public OrbitPredictor orbitPredictor;

    public Text textArea;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        OrbitUniversal ou = orbitPredictor.GetOrbitUniversal();
        string coeText = string.Format("Classical Orbit Elements\n");
        coeText += string.Format("a = {0:###.000}\n", ou.GetMajorAxis());
        coeText += string.Format("e = {0:0.0000}\n", ou.eccentricity);
        coeText += string.Format("i = {0:###.0000}\n", ou.inclination);
        coeText += string.Format("raan \u03a9 = {0:###.00}\n", ou.omega_uc);
        coeText += string.Format("argp \u03c9 = {0:###.00}\n", ou.omega_lc % 360.0f);
        coeText += string.Format("phase \u03bd = {0:###.00}\n", ou.phase);
        coeText += string.Format("Type = {0}", ou.PredictedOrbitType());
        coeText += string.Format("\nWhen e \u2248 0:\n");
        coeText += string.Format("(\u03c9 + \u03bd) ={0:###.00}", (ou.omega_lc + ou.phase)%360.0f);
        coeText += string.Format("\nWhen i \u2248 0:\n");
        coeText += string.Format("\u03d6 = (\u03a9 + \u03c9) ={0:###.00}", (ou.omega_uc + ou.omega_lc)%360.0f);
        textArea.text = coeText;
    }
}
