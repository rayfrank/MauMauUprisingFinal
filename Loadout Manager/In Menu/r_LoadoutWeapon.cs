using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    [CreateAssetMenu(fileName = "Loadout Weapon", menuName = "Loadout/Create loadout weapon")]
    public class r_LoadoutWeapon : ScriptableObject
    {
        #region Public Variables
        [Header("Loadout Weapon Information")]
        [SerializeField] string m_LoadoutWeaponName;
        [SerializeField] int m_LoadoutWeaponID;

        [Header("Loadout Weapon Type")]
        [SerializeField] r_LoadoutType m_LoadoutWeaponType;

        [Header("Loadout Weapon UI")]
        [SerializeField] Texture2D m_LoadoutWeaponTexture;
        #endregion

        #region Get
        public string GetLoadoutWeaponName() => this.m_LoadoutWeaponName;
        public int GetLoadoutWeaponID() => this.m_LoadoutWeaponID;
        public r_LoadoutType GetLoadoutWeaponType() => this.m_LoadoutWeaponType;
        public Texture2D GetLoadoutWeaponTexture() => this.m_LoadoutWeaponTexture;
        #endregion
    }
}