using UnityEngine;

/// <summary>
/// Interface for providing custom acceleration terms that act in addition to the
/// N-body gravity calculated by <see cref="GravityEngine"/>.
/// </summary>
public interface GEExternalAcceleration
{
    /// <summary>
    /// Compute an additional acceleration term to be applied to the body currently being
    /// evolved by the integrator.
    /// </summary>
    /// <param name="time">Simulation time in GE internal units.</param>
    /// <param name="gravityState">Current gravity state snapshot.</param>
    /// <param name="massKg">
    /// Optional updated mass. Implementations that model engines can reduce the mass and
    /// communicate the new value back through this parameter.
    /// </param>
    /// <returns>Acceleration vector expressed in GE internal units.</returns>
    double[] acceleration(double time, GravityState gravityState, ref double massKg);
}

