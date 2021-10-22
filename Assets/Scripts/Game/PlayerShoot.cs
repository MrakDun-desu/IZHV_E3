using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player shooting functionality.
/// </summary>
public class PlayerShoot : MonoBehaviour
{
    /// <summary>
    /// Gun currently wielded by the Player.
    /// </summary>
    [ Header("Global") ]
    public GameObject gun;
    
    // Input System Callbacks: 
    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (ctx.started) { gun.GetComponent<Gun>().StartFiring(); }
        if (ctx.canceled) { gun.GetComponent<Gun>().StopFiring(); }
    }
    
    /// <summary>
    /// Change target of the shooting.
    /// </summary>
    /// <param name="target">Target we are shooting at.</param>
    public void ChangeTarget(Vector3 target)
    { gun.GetComponent<Gun>().ChangeTarget(target); }

}
