
using UnityEngine;

public interface GEMapToSceneInterface {

    /// <summary>
    /// An optional element that can be used by GE to perform a mapping of all objects to be placed in the scene
    /// (nbodies, orbit predictors etc.). 
    /// 
    /// Useful in cases where a mapping more complex than a simple Unity transform is to be used (e.g. logarithmic distance scale). 
    /// 
    /// If a linear mapping is needed, "Use transform to reposition" and configuring the transform on the GE object may be a better
    /// solution.
    /// 
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    Vector3 MapToScene(Vector3 pos);

    Vector3 UnmapFromScene(Vector3 pos);
}
