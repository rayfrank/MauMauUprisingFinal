using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    public class m_SpectatorController : MonoBehaviour
    {
        public static m_SpectatorController instance;

        public delegate void _EventOnDie(string shooter, string hitter, float _attackerHealth, string _attackerWeaponName);
        public event _EventOnDie m_EventOnDie;

        #region Functions
        private void Awake()
        {
            if (instance)
            {
                Destroy(instance);
                Destroy(instance.gameObject);
            }
            instance = this;
        }

        private void OnEnable() => m_EventOnDie += OnDie;
        private void OnDisable() => m_EventOnDie -= OnDie;
        #endregion

        #region Events
        public void CallEventOnDie(string _Shooter, string hitter, float _attackerHealth, string _attackerWeaponName)
        {
            if (m_EventOnDie != null)
                m_EventOnDie(_Shooter, hitter, _attackerHealth, _attackerWeaponName);
        }
        #endregion

        #region Set
        private void OnDie(string shooter, string hitter, float _attackerHealth, string _attackerWeaponName) => m_SpectatorHolder.instance.SetTarget(shooter, _attackerHealth, _attackerWeaponName);
        #endregion
    }
}