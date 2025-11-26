using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface GEMultiplayerInterface {

    /// <summary>
    /// An NBody has been added to the local GE. 
    /// 
    /// This information will be propagated to the server and other players. 
    /// 
    /// It is assumed this will not result in a network driven add of this object on the client that initiated the request. 
    /// </summary>
    /// <param name="nbody"></param>
    void AddedBody(GameObject go, NBody nbody);

    /// <summary>
    /// An NBody has been removed from the local GE. 
    /// 
    /// This information will be propagated to the server and other players. 
    /// 
    /// It is assumed this will not result in a network driven remove of this object on the client that initiated the request. 
    /// </summary>
    /// <param name="nbody"></param>
    void RemovedBody(GameObject body);

    /// <summary>
    /// Local GE has changed a KEPLER mode orbit attached to an NBody. 
    /// 
    /// The OrbitUniversal (R, V, t) tuple has already been updated internally. 
    /// 
    /// This allows the usual methods for orbit changes (TransferShip, SetVelocity etc.) to be used in the local 
    /// GE an only when the OrbitUniversal is altered via a new set of RVT values is this synched to the other players.
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="orbitUniversal"></param>
    void OrbitChanged(NBody nbody, OrbitUniversal orbitUniversal);

    /// <summary>
    /// Local GE has changed the state of an NBody (i.e. altered it's r or v, likely via the execution of a manuever)
    ///
    /// The local GE has an update R, V state for this NBody.
    /// 
    /// It is assumed this will not result in a network driven update of this object on the client that initiated the request. 
    /// </summary>
    /// <param name="nbody"></param>
    void StateChanged(NBody nbody);

}
