using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MercatorPathOfNBody : MonoBehaviour
{
    [SerializeField]
    private NBody nbody;

    [SerializeField]
    private NBody centerBody;

    [SerializeField]
    private MercatorMap map;

    [SerializeField]
    private float orbitsAhead;

    [SerializeField]
    // when track needs to move behind map to wrap around, move behind the map by this factor
    private float mapStichDepth = 1.0f;

    private LineRenderer lineR; 

    private GravityEngine ge;

    private double mu;

    // ditance threshold to check if the trail has jumped from one sode of the map to the other
    private float map_wrap;
    private Vector3 behind_map; 

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        ge.AddGEStartCallback(GEStartCallback);
        lineR = GetComponent<LineRenderer>();
        map_wrap = map.GetMapWrapDistance();
        behind_map = -mapStichDepth * map.GetMapNormal();
    }

    private void GEStartCallback()
    {
        mu = ge.GetMass(centerBody);
    }

    // This is an initial greedy implementation. If orbit is Kepler or known to unchanging in a spherical field, could
    // just add/remove points from the list. 
    void Update()
    {
        if (ge.IsSetup()) {
            Vector3d r0 = ge.GetPositionDoubleV3(nbody);
            Vector3d v0 = ge.GetVelocityDoubleV3(nbody);
            OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(r0, v0, centerBody, true);
            // prop needs to start before earliest time it is asked for
            OrbitPropagator orbitProp = new OrbitPropagator(r0, v0, 0.0, mu);
            double dt =  oe.period * orbitsAhead/(double)lineR.positionCount;
            double time = 0.0; 
            Vector3[] positions = new Vector3[lineR.positionCount];
            Vector3 pos; 
            Vector3d r, v;
            int i = 0; 
            while (i < lineR.positionCount) {
                (r, v) = orbitProp.PropagateToTime(time);
                pos = map.Project(r.ToVector3(), (float) time + ge.GetPhysicalTime());
                if (i > 0) {
                    if (Vector3.Distance(pos, positions[i-1]) > map_wrap) {
                        // if close to the end of the line, then just repeat point before wrap and bail
                        if (i+3 > lineR.positionCount) {
                            for (int j = i; j < lineR.positionCount; j++)
                                positions[j] = positions[i - 1];
                            pos = positions[i - 1];
                            break;
                        }
                        // point has wrapped the map. Need to repeat previous point below map, add this as once below and once above
                        // i.e. stiching path underneath map
                        positions[i] = positions[i - 1] - behind_map;
                        positions[i + 1] = pos - behind_map;
                        i += 2;
                    }
                }
                positions[i] = pos;
                i++;
                time += dt;
            }
            lineR.SetPositions(positions);

        }
    }
}
