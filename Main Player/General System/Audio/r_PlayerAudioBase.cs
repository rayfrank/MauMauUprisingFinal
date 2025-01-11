using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    #region Serializable classes
    [System.Serializable]
    public class r_SurfaceMoveSetting
    {
        public string m_State;

        [Space(10)]
        public r_MoveState m_MoveState;
        public float m_FootstepLength;
        public float m_FootstepVolume;
    }

    [System.Serializable] public enum r_SurfaceType { DEFAULT, SPECIFIC }

    [System.Serializable]
    public class r_Surface
    {
        [Header("Surface Configuration")]
        public string m_SurfaceName;
        public r_SurfaceType m_SurfaceType;

        [Space(10)] public List<Texture2D> m_Textures;
        [Space(10)] public AudioClip[] m_FootstepClips;

        [Header("Bullet Impact")]
        public GameObject m_BulletImpact;
        public AudioClip m_BulletImpactSound;

        public AudioClip[] GetFootstepClips() => this.m_FootstepClips;
        public AudioClip GetBulletImpactClip() => this.m_BulletImpactSound;
    }
    #endregion

    [CreateAssetMenu(menuName = "FPS Template/Footstep System/Create Footstep Configuration", fileName = "Footstep Configuration")]
    public class r_PlayerAudioBase : ScriptableObject
    {
        #region public variables
        [Space(10)] public List<r_Surface> m_Surfaces = new();

        [Space(10)] public List<r_SurfaceMoveSetting> m_FootstepMoveSettings = new();

        [Header("Slide Audio")]
        public AudioClip m_SlideClip;

        [Header("Jump Audio")]
        public AudioClip m_JumpSound;
        [Range(0, 1)] public float m_JumpAudioVolume;

        [Header("Hurt Audio")]
        public AudioClip m_HurtSound;
        [Range(0, 1)] public float m_HurtAudioVolume;

        [Header("Bullet Audio Settings")]
        [Range(0, 1)] public float m_BulletImpactAudioVolume;
        #endregion
    }
}