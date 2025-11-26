
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Demo of code to handle an SGP4 satellite decay and reactivation when the time is jumped to an earlier pre-decay time. 
/// 
/// The script automatically adds an object using a prefab and it will decay within about 10 seconds game time after starting.
/// </summary>
public class AddSatThatDecays : MonoBehaviour
{
    [Header("Script adds SGP4 after GE start")]
    public GameObject prefab;

    public NBody earth;

    private GameObject toRemove;

    // Start is called before the first frame update
    void Start()
    {
        GravityEngine.Instance().AddGEStartCallback(GEStarted);
        toRemove = null;
    }

    private double decayTime;
    private void DecayCallback(OrbitUniversal ou, int error)
    {
        decayTime = GravityEngine.Instance().GetPhysicalTime();
        Debug.LogFormat("{0} deactivated at next Update t={1}", gameObject.name, decayTime);
        // This is called from within the physics code, so need to delay the removal until we're on the Update() cycle
        if (toRemove == null)
            toRemove = ou.gameObject;
    }

    private void GEStarted()
    {
        GameObject go = Instantiate(prefab);
        GravityEngine ge = GravityEngine.instance;
        OrbitUniversal ou = go.GetComponent<OrbitUniversal>();
        ou.centerNbody = earth;
        OrbitData od = new OrbitData();
        od.a = 6900 * GravityEngine.Instance().lengthScale;
        od.ecc = 0.1549f;
        od.centralMass = earth;
        od.mu = (float) ge.GetMass(earth);
        od.phase = 270f;
        ou.InitFromOrbitData(od, 0.0);
        ou.AddSGP4ErrorCallback(DecayCallback);
        // orbit predictor
        OrbitPredictor op = go.GetComponentInChildren<OrbitPredictor>();
        op.centerBody = earth.gameObject;
        op.body = go;
        ge.AddBody(go);
    }

    public void Activate(NBody nbody)
    {
        if (!nbody.gameObject.activeInHierarchy) {
            nbody.gameObject.SetActive(true);
            GravityEngine.Instance().SetNoUpdateFlag(nbody.gameObject, false);
            Debug.LogFormat("{0} reactivated at t={1}", gameObject.name, GravityEngine.Instance().GetPhysicalTime());
        } else {
            Debug.LogWarningFormat("{0} already active at t={1}", gameObject.name, GravityEngine.Instance().GetPhysicalTime());
        }
    }

    public void Deactivate(NBody nbody)
    {
        if (nbody.gameObject.activeInHierarchy) {
            nbody.gameObject.SetActive(false);
            GravityEngine.Instance().SetNoUpdateFlag(nbody.gameObject, true);
            Debug.LogFormat("{0} inactivated at t={1}", gameObject.name, GravityEngine.Instance().GetPhysicalTime());
        } else {
            Debug.LogWarningFormat("{0} already inactive at t={1}", gameObject.name, GravityEngine.Instance().GetPhysicalTime());
        }
    }
    private void Update()
    {

        if(toRemove != null) {
            // timeSlider MAY have jumped us to an earlier time since the decay started, in which case skip this
            if (GravityEngine.Instance().GetPhysicalTime() >= decayTime) {
                KeplerSequence kSeq = toRemove.GetComponent<KeplerSequence>();
                Debug.LogFormat("Add inactive KeplerSeq for {0} at t={1}", gameObject.name, decayTime);
                kSeq.AppendInactive(decayTime, Deactivate, Activate);
            }
            toRemove = null;
        }
      
    }

}
