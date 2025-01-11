using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Realtime;
using Photon.Pun;

namespace ForceCodeFPS
{
    #region Serializable Classes
    [System.Serializable]
    public class r_MenuPanel
    {
        [Header("Panel Information")]
        public string m_PanelName;
        public GameObject m_Panel;

        [Header("Panel Buttons")]
        public Button m_OpenButton;
        public Button m_CloseButton;

        [Space(10)] public bool m_Default;
    }
    #endregion

    public class r_MenuManager : MonoBehaviour
    {
        #region Public variables
        [Header("Menu Panels")]
        public List<r_MenuPanel> m_Panels = new List<r_MenuPanel>();

        [Header("Username UI")]
        public InputField m_UsernameInput;
        public Button m_UsenameSaveButton;

        [Header("Panels to hide")]
        public GameObject[] m_HidingPanels;
        #endregion

        #region Functions
        private void Awake() => HandleButtons();

        private void Start() => SetDefaults();
        #endregion

        #region Actions
        private void SetDefaults()
        {
            CheckUsername();
            SetDefaultMenuPanel();

            //Reset Time scale
            if (Time.timeScale != 1)
            {
                //Default time scale is 1 
                Time.timeScale = 1;
            }
        }

        private void CheckUsername()
        {
            if (PhotonNetwork.IsConnected)
            {
                if (PlayerPrefs.HasKey("username"))
                {
                    PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("username");
                    this.m_UsernameInput.text = PlayerPrefs.GetString("username");
                }
                else this.m_UsernameInput.text = "Player" + Random.Range(1, 999);
            }
        }

        private void SaveUsername(string _Username)
        {
            PlayerPrefs.SetString("username", _Username);

            if (PhotonNetwork.IsConnected)
                PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("username");
        }

        private void SetDefaultMenuPanel()
        {
            foreach (r_MenuPanel _panel in this.m_Panels)
            {
                if (_panel.m_Default)
                {
                    //Disable Hide panels
                    HandleHidingPanels(false);

                    //Enable panel if default
                    _panel.m_Panel.SetActive(true);
                }
            }
        }

        private void HandleHidingPanels(bool _state)
        {
            foreach (GameObject _panel in this.m_HidingPanels)
                _panel.SetActive(_state);
        }

        private void HandleButtons()
        {
            this.m_UsenameSaveButton.onClick.AddListener(delegate
            {
                if (!string.IsNullOrEmpty(this.m_UsernameInput.text))
                {
                    SaveUsername(this.m_UsernameInput.text);
                }
                r_AudioController.instance.PlayClickSound();
            });

            foreach (r_MenuPanel _panel in this.m_Panels)
            {
                _panel.m_OpenButton.onClick.AddListener(delegate { _panel.m_Panel.SetActive(true); HandleHidingPanels(false); r_AudioController.instance.PlayClickSound(); });
                _panel.m_CloseButton.onClick.AddListener(delegate { _panel.m_Panel.SetActive(false); HandleHidingPanels(true); r_AudioController.instance.PlayClickSound(); });
            }
        }
        #endregion
    }
}