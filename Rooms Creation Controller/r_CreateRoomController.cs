using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ForceCodeFPS
{
    /// <summary>
    /// This serializable class is used to add different maps with game modes.
    /// </summary>
    #region Game Map Class
    [System.Serializable]
    public class GameMap
    {
        public string m_MapName;
        public Sprite m_MapImage;
    }
    #endregion

    public class r_CreateRoomController : MonoBehaviour
    {
        public static r_CreateRoomController instance;

        #region Variables
        [Header("Player Limit")]
        public byte[] m_PlayerLimit;[HideInInspector] public int m_CurrentPlayerLimit;

        [Header("Game Modes")]
        public string[] m_GameModes;[HideInInspector] public int m_CurrentGameMode;

        [Header("Game Maps")]
        public GameMap[] m_GameMaps;[HideInInspector] public int m_CurrentGameMap;

        [Header("UI")]
        public r_CreateRoomControllerUI m_RoomUI;
        #endregion

        #region Unity Calls
        private void Awake()
        {
            if (instance)
            {
                Destroy(instance);
                Destroy(instance.gameObject);
            }

            instance = this;

            HandleButtons();
            UpdateUI();
        }
        #endregion

        /// <summary>
        /// In this section we are handling what de buttons does, and updating the UI information which display in the main menu.
        /// </summary>
        #region Handle UI
        private void HandleButtons()
        {
            //Change Map Buttons
            m_RoomUI.m_NextGameMapButton.onClick.AddListener(delegate { NextGameMap(true); r_AudioController.instance.PlayClickSound(); });
            m_RoomUI.m_PreviousGameMapButton.onClick.AddListener(delegate { NextGameMap(false); r_AudioController.instance.PlayClickSound(); });

            //Change Game Mode Buttons
            m_RoomUI.m_NextGameModeButton.onClick.AddListener(delegate { NextGameMode(true); r_AudioController.instance.PlayClickSound(); });
            m_RoomUI.m_PreviousGameModeButton.onClick.AddListener(delegate { NextGameMode(false); r_AudioController.instance.PlayClickSound(); });

            //Change Player Limit Buttons 
            m_RoomUI.m_NextPlayerLimitButton.onClick.AddListener(delegate { NextPlayerLimit(true); r_AudioController.instance.PlayClickSound(); });
            m_RoomUI.m_PreviousPlayerLimitButton.onClick.AddListener(delegate { NextPlayerLimit(false); r_AudioController.instance.PlayClickSound(); });

            //Create Room Button
            m_RoomUI.m_CreateRoomButton.onClick.AddListener(delegate { r_PhotonHandler.instance.CreateRoom(m_RoomUI.m_RoomNameInput.text, SetRoomOptions(false)); r_AudioController.instance.PlayClickSound(); m_RoomUI.m_CreateRoomButton.interactable = false; });
        }

        private void UpdateUI()
        {
            if (m_RoomUI == null) return;

            m_RoomUI.m_GameMapText.text = m_GameMaps[m_CurrentGameMap].m_MapName;
            m_RoomUI.m_GameModesText.text = m_GameModes[m_CurrentGameMode].ToString();
            m_RoomUI.m_PlayerLimitText.text = m_PlayerLimit[m_CurrentPlayerLimit].ToString() + " Players";
            m_RoomUI.m_MapImage.sprite = m_GameMaps[m_CurrentGameMap].m_MapImage;
        }
        #endregion

        /// <summary>
        /// In this section we are applying the room settings and properties.
        /// </summary>
        #region Set Room Options
        public RoomOptions SetRoomOptions(bool _RandomRoomOptions)
        {
            RoomOptions _RoomOptions = new RoomOptions
            {
                IsVisible = true,
                IsOpen = true,
                MaxPlayers = _RandomRoomOptions ? (byte)8 : m_PlayerLimit[m_CurrentPlayerLimit],
            };

            _RoomOptions.CustomRoomProperties = new Hashtable();

            int _RandomMapID = Random.Range(0, m_GameMaps.Length);

            _RoomOptions.CustomRoomProperties.Add(r_RoomProperties.RoomMapProperty, _RandomRoomOptions ? m_GameMaps[_RandomMapID].m_MapName : m_GameMaps[m_CurrentGameMap].m_MapName);
            _RoomOptions.CustomRoomProperties.Add(r_RoomProperties.RoomMapImageIDProperty, _RandomRoomOptions ? _RandomMapID : m_CurrentGameMap);
            _RoomOptions.CustomRoomProperties.Add(r_RoomProperties.RoomGameModeProperty, _RandomRoomOptions ? m_GameModes[Random.Range(0, m_GameModes.Length)] : m_GameModes[m_CurrentGameMode]);
            _RoomOptions.CustomRoomProperties.Add(r_RoomProperties.RoomStateProperty, _RandomRoomOptions ? "InLobby" : "InGame");

            string[] _CustomLobbyProperties = new string[4];

            _CustomLobbyProperties[0] = r_RoomProperties.RoomMapProperty;
            _CustomLobbyProperties[1] = r_RoomProperties.RoomMapImageIDProperty;
            _CustomLobbyProperties[2] = r_RoomProperties.RoomGameModeProperty;
            _CustomLobbyProperties[3] = r_RoomProperties.RoomStateProperty;

            _RoomOptions.CustomRoomPropertiesForLobby = _CustomLobbyProperties;

            return _RoomOptions;
        }
        #endregion

        /// <summary>
        /// In the sections below we are updating the UI display.
        /// </summary>
        #region Change Game Map
        private void NextGameMap(bool _Next)
        {
            if (_Next)
            {
                m_CurrentGameMap++;
                if (m_CurrentGameMap >= m_GameMaps.Length) m_CurrentGameMap = 0;
            }
            else
            {
                m_CurrentGameMap--;
                if (m_CurrentGameMap < 0) m_CurrentGameMap = m_GameMaps.Length - 1;
            }

            UpdateUI();
        }
        #endregion

        #region Change Game Mode
        private void NextGameMode(bool _Next)
        {
            if (_Next)
            {
                m_CurrentGameMode++;
                if (m_CurrentGameMode >= m_GameModes.Length) m_CurrentGameMode = 0;
            }
            else
            {
                m_CurrentGameMode--;
                if (m_CurrentGameMode < 0) m_CurrentGameMode = m_GameModes.Length - 1;
            }

            UpdateUI();
        }
        #endregion

        #region Change Player Limit
        private void NextPlayerLimit(bool _Next)
        {
            if (_Next)
            {
                m_CurrentPlayerLimit++;
                if (m_CurrentPlayerLimit >= m_PlayerLimit.Length) m_CurrentPlayerLimit = 0;
            }
            else
            {
                m_CurrentPlayerLimit--;
                if (m_CurrentPlayerLimit < 0) m_CurrentPlayerLimit = m_PlayerLimit.Length - 1;
            }

            UpdateUI();
        }
        #endregion
    }
}