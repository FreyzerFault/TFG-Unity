﻿using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DroneSim
{
    [RequireComponent(typeof(AudioSource))]
    public class Rotor : MonoBehaviour
    {
        // Drone (parent)
        [HideInInspector] public DroneController drone;
        private Rigidbody drone_rb;

        // Animation Active
        public bool animationActivated = true;
        public bool blurActivated = true;


        private MeshRenderer meshRenderer;
        private MeshRenderer blurMeshRenderer;
        public Texture2D[] blurTextures;

        // Clockwise / CounterClockwise
        public bool counterclockwise = false;

        // Rotor Engine Power [0,1]
        [HideInInspector] public float power;
        public bool smoothAnimation = true;

        private float lastPower;
        protected float SmoothPower;
        private float smoothStep = 0.1f;
        public float minSmoothPower = 0.00001f;

        public virtual float Power => smoothAnimation ? SmoothPower : power;


        protected virtual void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();

            if (transform.childCount > 0)
                blurMeshRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();

            audioSource = GetComponent<AudioSource>();

            lastPower = power;
            SmoothPower = power;
        }

        protected virtual void Start()
        {
            drone = DroneManager.Instance.currentDroneController;
            drone_rb = drone.GetComponent<Rigidbody>();

            GameManager.Instance.OnPause += OnPause;
            GameManager.Instance.OnUnpause += OnUnpause;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPause -= OnPause;
                GameManager.Instance.OnUnpause -= OnUnpause;
            }
        }

        #region OnPause

        private void OnPause()
        {
            audioSource.mute = true;
            enabled = false;
        }

        private void OnUnpause()
        {
            audioSource.mute = false;
            enabled = true;
        }

        #endregion

        protected virtual void Update()
        {
            // Smooth Power value for animation and audio
            if (smoothAnimation)
            {
                UpdateSmoothPower();
            }
            else
                SmoothPower = power;

            // Animation
            if (animationActivated)
            {
                AnimatePropeller(SmoothPower);
                if (blurActivated)
                    SetTexture(SmoothPower);
            }

            // Audio
            if (audioActivated) SetAudio(SmoothPower);
        }

        

        protected virtual void FixedUpdate()
        {
            // CLAMP Power [0,1]
            power = Mathf.Clamp01(power);

            // Force upwards to drone from rotor point
            ApplyThrottle();
            ApplyTorque();
        }

        // Update power used for animations, it changes smoothly to simulate a real propeller
        protected void UpdateSmoothPower()
        {
            // CLAMP Power [0,1]
            power = Mathf.Clamp01(power);

            // Smooth change in power
            float powerDiff = power - lastPower;

            // Si la diferencia es negativa, esta disminuyendo, visualmente no deben verse las helices frenadas de golpe,
            // por lo que hay que utilizar un smoothStep mucho mas pequeño, para reducir el frenado
            float breakSmoothStep = smoothStep * lastPower / 8;
            if (powerDiff < 0 && Mathf.Abs(powerDiff) > breakSmoothStep)
                SmoothPower = lastPower - breakSmoothStep;
            else
            {
                if (Mathf.Abs(powerDiff) > smoothStep)
                    SmoothPower = lastPower + smoothStep * (powerDiff > 0 ? 1 : -1);
                else
                    SmoothPower = power;
            }

            lastPower = Mathf.Max(minSmoothPower, SmoothPower);
        }

        #region Animation
        
        private float MaxRotationSpeed => drone.droneSettings.maxRotationSpeed; 
        protected virtual void AnimatePropeller(float power_t)
        {
            float angle = Mathf.Lerp(0, MaxRotationSpeed, power_t) * Time.deltaTime *
                          (counterclockwise ? -1 : 1);
            transform.RotateAround(transform.position, drone.transform.up, angle);
        }

        #endregion

        #region BlurTexture

        // Select a Blur Propeller Texture depending on power
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

        #endregion

        #region Audio

        public bool audioActivated = true;
        public float maxVolume = 0.5f;
        private AudioSource audioSource;

        protected void SetAudio(float power_t)
        {
            float powerSqr = power_t * power_t;
            audioSource.volume = Mathf.Lerp(0, maxVolume, powerSqr);
            audioSource.pitch = Mathf.Lerp(0.9f, 1.1f, powerSqr);
        }

        #endregion

        #region Physics
        
        // Torque = Rotational Force applied to propeller by rotor (CW > 0, CCW < 0)
        public float Torque => power * MaxTorque * (counterclockwise ? -1 : 1);

        // Throttle = upward force (Power = 0.5 => Hover => Throttle = Gravity)
        public float Throttle => power * MaxThrottle;

        private float MaxTorque => drone.droneSettings.maxTorque;
        private float MaxThrottle => drone.droneSettings.maxThrottle;
        
        // Throttle = upward force caused by air flowing down
        private void ApplyThrottle() => drone_rb.AddForceAtPosition(drone.transform.up * Throttle, transform.position);

        /// <summary>
        /// Torque is based in 3º law of Newton
        /// <para>Action-Reaction principle: For every action there is an equal and opposite reaction</para>
        /// <para>Torque applied to propeller will apply a inverse torque to drone</para>
        /// </summary>
        private void ApplyTorque() => drone_rb.AddTorque(drone.transform.up * -Torque);

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