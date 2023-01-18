﻿using UnityEngine;

namespace DroneSim
{
    [RequireComponent(typeof(AudioSource))]
    public class Rotor : MonoBehaviour
    {
        // Drone (parent)
        public DroneController drone;
        private Rigidbody drone_rb;

        // Animation Active
        public bool animationActivated = true;
        public bool blurActivated = true;

        private MeshRenderer meshRenderer;
        private MeshRenderer blurMeshRenderer;
        public Texture2D[] blurTextures;

        protected AudioSource audioSource;

        // Orientation
        public bool counterclockwise = false;

        // Rotor Engine Power [0,1]
        public float power;
        public bool smoothAnimation = true;

        private float lastPower;
        protected float smoothPower;
        private float smoothStep = 0.1f;

        public float Power => smoothAnimation ? smoothPower : power;

        #region Physics Parameters

        // Torque = Rotational Force applied to propeller by rotor (CW > 0, CCW < 0)
        public float Torque => power * maxTorque * (counterclockwise ? -1 : 1);

        // Throttle = upward force (Power = 0.5 => Hover => Throttle = Gravity)
        public float Throttle => power * maxThrottle;


        private float maxRotationSpeed => drone.droneSettings.maxRotationSpeed;
        private float maxTorque => drone.droneSettings.maxTorque;
        private float maxThrottle => drone.droneSettings.maxThrottle;

        #endregion



        protected virtual void Awake()
        {
            Transform t = transform;
            if (t.parent != null && t.parent.GetComponent<DroneController>())
            {
                drone = t.parent.GetComponent<DroneController>();
                drone_rb = drone.GetComponent<Rigidbody>();
            }
            
            if (drone == null)
                Debug.LogError("Dron not found");

            meshRenderer = GetComponent<MeshRenderer>();
            blurMeshRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();

            audioSource = GetComponent<AudioSource>();

            lastPower = power;
            smoothPower = power;
        }

        protected virtual void Update()
        {
            // Smooth Power value for animation and audio
            if (smoothAnimation)
            {
                UpdateSmoothPower();
            }
            else
                smoothPower = power;

            // Animation
            if (animationActivated)
            {
                AnimatePropeller(smoothPower);
                if (blurActivated)
                    SetTexture(smoothPower);
            }

            // Audio
            SetAudio(smoothPower);
        }

        protected void UpdateSmoothPower()
        {
            // CLAMP Power [0,1]
            power = Mathf.Clamp01(power);

            // Smooth change in power
            float powerDiff = power - lastPower;
            if (Mathf.Abs(powerDiff) > smoothStep)
                smoothPower = lastPower + smoothStep * (powerDiff > 0 ? 1 : -1);
            else
                smoothPower = power;

            lastPower = smoothPower;
        }

        protected virtual void FixedUpdate()
        {
            // CLAMP Power [0,1]
            power = Mathf.Clamp01(power);
            
            // Force upwards to drone from rotor point
            ApplyThrottle();
            ApplyTorque();
        }


        #region Animation

        /// <summary>
        /// Rotate depending on power
        /// </summary>
        /// <param name="power_t"></param>
        protected void AnimatePropeller(float power_t)
        {
            float maxRotationSpeed = drone.droneSettings.maxRotationSpeed;
            float angle = Mathf.Lerp(0, maxRotationSpeed, Mathf.Pow(power_t, 0.1f)) * Time.deltaTime *
                          (counterclockwise ? -1 : 1);
            transform.RotateAround(transform.position, drone.transform.up, angle);
        }

        /// <summary>
        /// Change texture dynamicaly depending on power
        /// </summary>
        /// <param name="power_t"></param>
        protected void SetTexture(float power_t)
        {
            float minRotationForBlur = 0.1f;
            // If power < 0.5, hide propeller and show blur propeller quad
            meshRenderer.enabled = power_t < minRotationForBlur;
            blurMeshRenderer.enabled = power_t >= minRotationForBlur;

            // Switch between blur textures by power
            if (power_t >= minRotationForBlur)
            {
                Texture2D tex = blurTextures[0];
                if (power_t >= 0.6f)
                    tex = blurTextures[1];
                
                blurMeshRenderer.sharedMaterial.mainTexture = tex;
            }
        }

        /// <summary>
        /// Change audio params dynamicaly depending on power
        /// </summary>
        /// <param name="power_t"></param>
        private void SetAudio(float power_t)
        {
            float powerSqr = power_t * power_t;
            audioSource.volume = Mathf.Lerp(0, .5f, powerSqr);
            audioSource.pitch = Mathf.Lerp(0.9f, 1.1f, powerSqr);
        }

        #endregion

        #region Physics

        /// <summary>
        /// Throttle = upward force caused by air flowing down
        /// </summary>
        private void ApplyThrottle()
        {
            drone_rb.AddForceAtPosition(drone.transform.up * Throttle, transform.position);
        }

        /// <summary>
        /// Torque is based in 3º law of Newton
        /// <para>Action-Reaction principle: For every action there is an equal and opposite reaction</para>
        /// <para>Torque applied to propeller will apply a inverse torque to drone</para>
        /// </summary>
        private void ApplyTorque()
        {
            drone_rb.AddTorque(drone.transform.up * -Torque);
        }

        #endregion


        #region Gizmos

        private void OnDrawGizmos()
        {
            // Gizmos.color = Color.Lerp(Color.red, Color.green, power);
            //
            // Gizmos.DrawLine(transform.position, transform.position + transform.forward * (power));

            // Gizmos.color = Color.magenta;
            // Gizmos.DrawLine(transform.position, transform.position + transform.forward * Torque);
        }

        #endregion

    }
}
