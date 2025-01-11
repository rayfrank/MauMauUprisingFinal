using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    #region Serializable Enums
    [System.Serializable] public enum r_LoadoutType { PRIMARY, SECONDARY, LETHAL, TACTICAL }
    #endregion

    #region Serializable Classes
    [System.Serializable]
    public class r_LoadoutPreview
    {
        [Header("Loadout Information")]
        public r_LoadoutType m_LoadoutType;

        [Header("Loadout Display UI")]
        public RawImage m_LoadoutPreviewImage;
    }
    #endregion

    public class r_LoadoutManagerMenu : MonoBehaviour
    {
        #region Static Variables
        public static r_LoadoutManagerMenu instance;
        #endregion

        #region Public Variables
        [Header("Loadout UI")]
        [SerializeField] private Dropdown m_LoadoutDropdown;

        [SerializeField, Space(10)] private List<r_LoadoutWeaponClass> m_LoadoutClasses = new List<r_LoadoutWeaponClass>();

        [SerializeField, Space(10)] private List<r_LoadoutPreview> m_LoadoutPreviews = new List<r_LoadoutPreview>();

        [Header("Tracking")]
        public int m_SelectedLoadoutIndex = 0;
        #endregion

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

        private void Start()
        {
            PopulateLoadoutSelection();
            PreviewLoadout(this.m_SelectedLoadoutIndex);

            this.m_LoadoutDropdown.onValueChanged.AddListener(delegate
            {
                SelectLoadout(this.m_LoadoutDropdown.value);
                PreviewLoadout(this.m_SelectedLoadoutIndex);
            });
        }
        #endregion

        #region Actions
        private void PopulateLoadoutSelection()
        {
            List<string> _loadout_classes = new List<string>();

            for (int i = 0; i < this.m_LoadoutClasses.Count; i++)
            {
                //Get all class names to use in dropdown
                _loadout_classes.Add(this.m_LoadoutClasses[i].GetLoadoutName());
            }

            this.m_LoadoutDropdown.AddOptions(_loadout_classes);
        }

        private void SelectLoadout(int _loadout_index) => this.m_SelectedLoadoutIndex = _loadout_index;

        private void PreviewLoadout(int _loadout_index)
        {
            r_LoadoutWeaponClass _loadout_class = this.m_LoadoutClasses[_loadout_index];

            if (_loadout_class != null)
            {
                foreach (r_LoadoutPreview _loadout_preview in this.m_LoadoutPreviews)
                {
                    //Get loadout by type for each preview type
                    r_LoadoutWeapon _loadout_weapon = _loadout_class.GetLoadoutByType(_loadout_preview.m_LoadoutType);

                    if (_loadout_weapon != null)
                    {
                        //Enable image preview
                        _loadout_preview.m_LoadoutPreviewImage.enabled = true;

                        //Load loadout weapon texture
                        _loadout_preview.m_LoadoutPreviewImage.texture = _loadout_weapon.GetLoadoutWeaponTexture();
                    }
                    else
                    {
                        //Disable image preview
                        _loadout_preview.m_LoadoutPreviewImage.enabled = false;
                    }
                }
            }
        }

        public void SaveLoadoutWeapon(r_LoadoutWeapon _loadout_weapon)
        {
            r_LoadoutWeaponClass _loadout_class = this.m_LoadoutClasses[this.m_SelectedLoadoutIndex];

            if (_loadout_class != null && _loadout_weapon != null)
            {
                if (_loadout_class.GetLoadoutByType(_loadout_weapon.GetLoadoutWeaponType()))
                {
                    //Remove current loadout weapon by type
                    _loadout_class.RemoveLoadoutWeaponByType(_loadout_weapon.GetLoadoutWeaponType());
                }

                //Add if we don't have the same loadout type already
                _loadout_class.m_LoadoutWeapons.Add(_loadout_weapon);
            }

            //Update loadout preview UI
            PreviewLoadout(this.m_SelectedLoadoutIndex);

            //Save loadout
#if UNITY_EDITOR
            EditorUtility.SetDirty(_loadout_class);
#endif
        }
        #endregion
    }
}