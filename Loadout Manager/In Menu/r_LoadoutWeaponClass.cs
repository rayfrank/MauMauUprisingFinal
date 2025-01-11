using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    [CreateAssetMenu(fileName = "Loadout Class", menuName = "Loadout/Create loadout class")]
    public class r_LoadoutWeaponClass : ScriptableObject
    {
        #region Public Variables
        [Header("Loadout information")]
        [SerializeField] string m_LoadoutName;

        [Space(10)] public List<r_LoadoutWeapon> m_LoadoutWeapons = new List<r_LoadoutWeapon>();
        #endregion

        #region Get
        public int[] GetLoadoutWeaponIDS()
        {
            int[] _weapon_ids = new int[this.m_LoadoutWeapons.Count];

            for (int i = 0; i < this.m_LoadoutWeapons.Count; i++)
            {
                _weapon_ids[i] = this.m_LoadoutWeapons[i].GetLoadoutWeaponID();
            }
            return _weapon_ids;
        }
        public string GetLoadoutName() => this.m_LoadoutName;
        public int GetLoadoutIndexByType(r_LoadoutType _loadout_type) => this.m_LoadoutWeapons.FindIndex(x => x.GetLoadoutWeaponType() == _loadout_type);
        public r_LoadoutWeapon GetLoadoutByType(r_LoadoutType _loadout_type) => this.m_LoadoutWeapons.Find(x => x.GetLoadoutWeaponType() == _loadout_type);
        #endregion

        #region Set
        public void RemoveLoadoutWeaponByType(r_LoadoutType _loadout_type) => this.m_LoadoutWeapons.RemoveAll(x => x.GetLoadoutWeaponType() == _loadout_type);
        #endregion
    }
}