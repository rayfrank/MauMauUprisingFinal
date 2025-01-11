using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    public class r_LoadoutManagerGame : MonoBehaviour
    {
        #region Static Variables
        public static r_LoadoutManagerGame instance;
        #endregion

        #region Public Variables
        [Header("Loadout Selection Content")]
        public Transform m_LoadoutContent;

        [Header("Loadout Preview Entry")]
        public r_LoadoutGameEntry m_LoadoutPrefab;

        [Header("Loadout Classes")]
        public List<r_LoadoutWeaponClass> m_LoadoutClasses = new List<r_LoadoutWeaponClass>();
        #endregion

        #region Private Variables
        [Header("Tracking")]
        public int m_SelectedLoadoutIndex = 0;

        [Header("Loadout Entries")]
        private List<r_LoadoutGameEntry> m_LoadoutEntries = new List<r_LoadoutGameEntry>();
        #endregion

        #region Functions
        private void Awake()
        {
            if (instance) Destroy(instance.gameObject);

            instance = this;
        }

        private void Start() => PreviewLoadouts();
        #endregion

        #region Actions
        private void PreviewLoadouts()
        {
            foreach (r_LoadoutWeaponClass _loadout_entry in this.m_LoadoutClasses)
            {
                //Instantiate loadout entry
                r_LoadoutGameEntry _loadout = (r_LoadoutGameEntry)Instantiate(this.m_LoadoutPrefab, this.m_LoadoutContent.transform);

                //Setup loadout preview
                _loadout.PreviewLoadout(_loadout_entry);

                //Add loadout entries
                this.m_LoadoutEntries.Add(_loadout);
            }
        }

        public void DeselectLoadoutButtons(string _except_loadout_name)
        {
            foreach (r_LoadoutGameEntry _loadout_entry in this.m_LoadoutEntries)
            {
                if (_loadout_entry.m_LoadoutClass != null)
                {
                    if (_loadout_entry.m_LoadoutClass.GetLoadoutName() != _except_loadout_name)
                    {
                        //Enable interactable to make it unselected
                        _loadout_entry.m_LoadoutSelectButton.interactable = true;

                        //Set loadout button text to select
                        _loadout_entry.m_LoadoutSelectButtonText.text = "SELECT";
                    }
                    else
                    {
                        //Set loadout button text to selected
                        _loadout_entry.m_LoadoutSelectButtonText.text = "SELECTED";
                    }
                }
            }
        }

        public void SelectLoadout(int _loadout_index) => this.m_SelectedLoadoutIndex = _loadout_index;
        #endregion
    }
}