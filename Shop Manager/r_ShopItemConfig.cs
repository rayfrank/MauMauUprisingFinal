using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    [System.Serializable, CreateAssetMenu(fileName = "Shop Item", menuName = "Shop/Create Shop Item")]
    public class r_ShopItemConfig : ScriptableObject
    {
        #region Public Variables
        [Header("Information")]
        public string m_ItemName;
        public string m_ItemDescription;

        [Header("Data")]
        public float m_ItemPrice;
        public int m_ItemIndex;

        [Header("Type")]
        public r_ShopItemType m_ItemType;

        [Header("Texture")]
        public Texture m_ItemTexture;

        [Header("Loadout Weapon")]
        public r_LoadoutWeapon m_LoadoutWeapon;

        [Space(10)] public bool m_IsPurchased;
        #endregion

    }
}