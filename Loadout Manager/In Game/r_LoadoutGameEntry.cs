using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    public class r_LoadoutGameEntry : MonoBehaviour
    {
        #region Public Variables
        [Header("Loadout Entry UI")]
        [Space(10)] public Text m_LoadoutNameDisplay;
        [Space(10)] public Text m_LoadoutSelectButtonText;
        [Space(10)] public Button m_LoadoutSelectButton;

        [Space(10)] public List<r_LoadoutPreview> m_LoadoutPreviews = new List<r_LoadoutPreview>();
        #endregion

        #region Private Variables
        [HideInInspector] public r_LoadoutWeaponClass m_LoadoutClass;
        #endregion

        #region Functions
        private void Awake()
        {
            this.m_LoadoutSelectButton.onClick.AddListener(delegate
            {
            //Select Loadout
            r_LoadoutManagerGame.instance.SelectLoadout(r_LoadoutManagerGame.instance.m_LoadoutClasses.FindIndex(x => x == m_LoadoutClass));

            //Unselect loadout buttons
            r_LoadoutManagerGame.instance.DeselectLoadoutButtons(this.m_LoadoutClass.GetLoadoutName());
            });
        }
        #endregion

        #region Actions
        public void PreviewLoadout(r_LoadoutWeaponClass _Loadout_class)
        {
            if (_Loadout_class != null)
            {
                this.m_LoadoutClass = _Loadout_class;

                this.m_LoadoutNameDisplay.text = this.m_LoadoutClass.GetLoadoutName();

                foreach (r_LoadoutPreview _loadout_preview in this.m_LoadoutPreviews)
                {
                    //Get loadout by type for each preview type
                    r_LoadoutWeapon _loadout_weapon = this.m_LoadoutClass.GetLoadoutByType(_loadout_preview.m_LoadoutType);

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
        #endregion
    }
}