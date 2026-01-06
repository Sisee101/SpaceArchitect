
using UnityEngine;

/// <summary>
/// Abstract class to hold information for a force delegate for a given NBody. 
/// 
/// An Nbody can add a component that extends this class. When the NBody is added to GE the
/// nbodyState will grab a reference to this base class if it is present. This can then be referenced
/// in the force delegate code to get parameters (e.g. value of J2 and rotation axis) for a given NBody.
/// 
/// </summary>
public abstract class ForceData : MonoBehaviour
{
    public enum ForceType { NONE, J2 };

    protected ForceType forceType = ForceType.NONE;
}
