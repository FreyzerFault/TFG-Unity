using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public DroneSettingsSO droneSettings;
    public EnvironmentSettingsSO environmentSettings;
    
    // Rotors of the drone (have to be associated to the four rotors of the drone, with the order V1,O1,V2,O2)
    public Rotor rotorCW1;
    public Rotor rotorCW2;
    public Rotor rotorCCW1;
    public Rotor rotorCCW2;
    
    
    [Range(0,1)] public float yaw = 0;
    [Range(0,1)] public float pitch = 0;
    [Range(0,1)] public float roll = 0;
    [Range(0,1)] public float lift = 0;

    private void FixedUpdate()
    {
        ApplyYaw();
        ApplyLift();
        
        Debug.Log("Lift: " + lift);
    }

    #region Physics

    private void ApplyLift()
    {
        // Same power to each rotor ([-1,1] -> [0,1])
        rotorCW1.power = lift / 2 + 0.5f;
        rotorCW2.power = lift / 2 + 0.5f;
        rotorCCW1.power = lift / 2 + 0.5f;
        rotorCCW2.power = lift / 2 + 0.5f;
    }
    
    /// <summary>
    /// YAW of the Drone
    /// <p>Calculate the torque generated by each rotor and applies it to the drone</p>
    /// <p>If the sum is 0, the drone will not rotate</p>
    /// <p>If the sum is 1, the drone will rotate by the difference between the CW and CCW rotors</p>
    /// </summary>
    private void ApplyYaw()
    {
        float torque = rotorCW1.Throttle + rotorCW2.Throttle + rotorCCW1.Throttle + rotorCCW2.Throttle;
        transform.Rotate(transform.up, torque * Time.fixedDeltaTime);
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        
        Gizmos.DrawLine(transform.position, transform.position + GetComponent<Rigidbody>().centerOfMass);
    }
}
