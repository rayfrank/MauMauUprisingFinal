using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    #region Serializable classes
    [System.Serializable] public enum r_MoveState { IDLE, JUMPING, WALKING, SPRINTING, CROUCHING, SLIDING }

    [System.Serializable]
    public class r_MoveStateSetting
    {
        [Header("Speed Setting")]
        public r_MoveState m_MoveState;
        public float m_MoveSpeed;
    }
    #endregion

    [CreateAssetMenu(menuName = "FPS Template/Player System/Create Controller Configuration", fileName = "Controller Configuration")]
    public class r_PlayerControllerBase : ScriptableObject
    {
        #region Public variables
        public List<r_MoveStateSetting> m_MoveStateSettings = new List<r_MoveStateSetting>();

        [Header("Speed settings")]
        public float m_AccelerationSpeed;

        [Header("Jump settings")]
        public float m_JumpHeight;
        public float m_MaxJumpCount;

        [Header("Wall jump settings")]
        public int m_MaxWallJumpCount;

        [Header("Physic Settings")]
        public float m_Gravity;

        [Header("Stick to ground")]
        public float m_StickToGroundForce;

        [Header("Slope slide settings")]
        public float m_slopeLimit;
        public float m_slopeSlideSpeed;

        [Header("Controller Height settings")]
        public float m_CrouchHeight;
        public float m_StandHeight;

        [Header("Camera Height settings")]
        public float m_CameraCrouchHeight;
        public float m_CameraStandHeight;
        public float m_CameraHeightAdjustSpeed;

        [Header("Stamina settings")]
        public bool m_staminaFeature;

        public float m_maxStamina;
        public float m_staminaUsable;

        [Space(10)]
        public float m_staminaRecoverSpeed;
        public float m_staminaReduceSpeed;

        [Header("Sliding settings")]
        public bool m_slideFeature;

        public float m_slideLength;
        public float m_slideStopLength;

        [Space(10)]
        public float m_slideTimeIncreaseMultiplier;
        public float m_slideTimeDecreaseMultiplier;

        [Space(10)]

        public float m_CanSlideLength;
        public float m_CanSlideJumpLength;
        #endregion
    }
}