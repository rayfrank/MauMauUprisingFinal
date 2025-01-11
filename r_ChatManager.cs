using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.IO;

namespace ForceCodeFPS
{
    public class r_ChatManager : MonoBehaviour
    {
        public static r_ChatManager Instance;

        #region Public Variables
        [Space(10)] public PhotonView m_PhotonView;

        [Header("Chat Panel")]
        public GameObject m_ChatPanel;

        [Header("Chat System")]
        public Transform m_ChatContent;
        public GameObject m_ChatPrefab;

        [Header("Chat Field")]
        public InputField m_ChatMessageInput;
        public Text m_ChatPlaceHolder;

        [Header("Chat UI Settings")]
        public Color m_UsernameColor;
        public Color m_DefaultTextColor;

        [Header("Chat Placeholder Settings")]
        public string m_ChatPlaceHolderText;

        [Header("Chat Settings")]
        public float m_ChatDuration;
        #endregion

        #region Private Variables
        //Current chat state
        [HideInInspector] public bool m_ChatOpened;
        #endregion

        #region Functions
        private void Awake()
        {
            if (Instance)
            {
                Destroy(Instance);
                Destroy(Instance.gameObject);
            }

            Instance = this;
        }

        private void Start() => SetDefault();

        private void Update() => HandleInputs();
        #endregion

        #region Actions
        private void SetDefault()
        {
            //Disable chat
            this.m_ChatOpened = false;

            //Update UI
            this.m_ChatPanel.SetActive(m_ChatOpened);

            //Set placeholder 
            this.m_ChatPlaceHolder.text = this.m_ChatPlaceHolderText;
        }

        private void HandleInputs()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (this.m_ChatOpened)
                {
                    if (!string.IsNullOrEmpty(this.m_ChatMessageInput.text))
                    {
                        //Send chat
                        SendChat(m_ChatMessageInput.text);
                    }

                    //Clean inputfield
                    this.m_ChatMessageInput.text = string.Empty;

                    //Update chat panel
                    UpdateChatPanel(false);
                }
                else
                {
                    //Update chat panel
                    UpdateChatPanel(true);

                    //Focus inputfield
                    this.m_ChatMessageInput.Select();
                    this.m_ChatMessageInput.ActivateInputField();
                }
            }
        }

        private void UpdateChatPanel(bool _state)
        {
            //Set state
            this.m_ChatOpened = _state;

            //Update UI
            this.m_ChatPanel.SetActive(m_ChatOpened);

            //Disable placeholder on chat opened
            this.m_ChatPlaceHolder.gameObject.SetActive(!m_ChatOpened);

            //Controllable
            if (r_InGameManager.Instance.m_CurrentPlayer != null)
            {
                r_InGameManager.Instance.m_CurrentPlayer.GetComponent<r_PlayerController>().m_InputManager.m_Controllable = !this.m_ChatOpened;
            }
        }

        public void SendChat(string _message) => this.m_PhotonView.RPC(nameof(RPC_SendChat), RpcTarget.All, PhotonNetwork.LocalPlayer.NickName, _message);
        #endregion

        #region Network Events
        [PunRPC]
        public void RPC_SendChat(string _player_name, string _message)
        {
            //Create new chat entry
            GameObject _entry = (GameObject)Instantiate(this.m_ChatPrefab, this.m_ChatContent.transform);

            //Player name color
            Color _color = PhotonNetwork.LocalPlayer.NickName == _player_name ? this.m_UsernameColor : this.m_DefaultTextColor;

            //Change color to RGBA
            string _color_RGBA = ColorUtility.ToHtmlStringRGBA(_color);

            //Set text
            _entry.GetComponentInChildren<Text>().text = $"<color=#{_color_RGBA}>{_player_name}</color> : {_message}";

            //Destroy killfeed
            Destroy(_entry, this.m_ChatDuration);
        }
        #endregion
    }
}