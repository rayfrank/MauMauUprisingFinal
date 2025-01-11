using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    #region Serializable Classes
    [System.Serializable] public enum r_CameraState { HIP, AIMING }

    [System.Serializable]
    public class r_MoveCameraSetting
    {
        public r_MoveState m_MoveState;

        [Header("Camera Settings")]
        public float m_CameraFOV;

        [Header("Headbob Settings")]
        public float m_HeadbobSpeed;
        public float m_HeadbobAmount;
    }

    [System.Serializable]
    public class r_CameraJumpLandSettings
    {
        public bool m_jumpLandFunction;

        [Header("Jump settings")]
        public Vector3 m_jumpPosition;
        public Vector3 m_jumpRotation;

        [Header("Land settings")]
        public Vector3 m_landPosition;
        public Vector3 m_landRotation;

        [Header("General settings")]
        public float m_effectDuration;
        public float m_effectDurationReturn;
    }

    [System.Serializable]
    public class r_CameraGeneralSettings
    {
        [Header("Mouse Look Settings")]
        public float m_MinMouseY;
        public float m_MaxMouseY;

        [Header("Mouse settings")]
        public float m_hipSensitivity;
        public float m_aimSensitivity;

        [Header("Camera FOV Settings")]
        public float m_cameraFOV;
        public float m_CameraFOVSpeed;
    }

    [System.Serializable]
    public class r_CameraHitReactionSettings
    {
        [Header("Hit Reaction Settings")]
        public float m_CameraHitForce;
        public float m_CameraHitDuration;
        public float m_CameraHitDamping;
    }

    [System.Serializable]
    public class r_CameraLeanSettings
    {
        public bool m_LeanFeature;

        [Header("Lean Settings")]
        public float m_LeanMoveAngle;
        public float m_LeanRotationAngle;

        [Header("Lean Smoothness")]
        public float m_LeanAngleSpeed;
        public float m_LeanRate;

        [Header("Lean Animator Settings")]
        public float m_MaxLeanAngleAnimator;
        public float m_AnimatorLeanChangeSpeed;
    }
    #endregion

    [CreateAssetMenu(menuName = "FPS Template/Player System/Create Camera Configuration", fileName = "Camera Configuration")]
    public class r_PlayerCameraBase : ScriptableObject
    {
        #region Variables
        [Space(10)] public List<r_MoveCameraSetting> m_MoveCameraSettings = new List<r_MoveCameraSetting>();

        [Space(10)] public r_CameraGeneralSettings m_CameraGeneralSettings;

        [Space(10)] public r_CameraJumpLandSettings m_CameraJumpLandSettings;

        [Space(10)] public r_CameraHitReactionSettings m_CameraHitReactionSettings;

        [Space(10)] public r_CameraLeanSettings m_CameraLeanSettings;
        #endregion
    }
}