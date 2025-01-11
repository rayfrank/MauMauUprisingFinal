using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    [CreateAssetMenu(menuName = "FPS Template/Player System/Create Health Configuration", fileName = "Health Configuration")]
    public class r_PlayerHealthBase : ScriptableObject
    {
        #region Public Variables
        [Header("Health settings")]
        public float m_MaxHealth;

        [Header("Health UI settings")]
        public float m_HealthWarning;

        [Header("Fall Damage settings")]
        public float m_FallDamageHeight;
        public float m_FallDamageMultiplier;
        #endregion
    }
}