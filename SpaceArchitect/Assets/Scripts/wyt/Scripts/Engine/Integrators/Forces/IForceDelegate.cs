using UnityEngine;
using System.Collections;

public interface IForceDelegate  {

    /// <summary>
    /// Calculate the acceleration between bodies i and j. 
    /// 
    /// Return the aij and aji accelerations because in some case (e.g one is oblate) they will not be equal and
    /// opposite!
    /// 
    /// </summary>
    /// <param name="rji"></param>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <param name="nbodyStates"></param>
    /// <returns></returns>
    double[] CalcAccelerationIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates);

    double[] CalcAccelerationIPerParticle(double[] rji, int i, GravityState.NbodyState[] nbodyStates);

    double[] CalcJerkIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates);
}
