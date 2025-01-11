using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    public class r_CreateRoomControllerUI : MonoBehaviour
    {
        #region Variables
        [Header("Display Map Image")]
        public Image m_MapImage;

        [Header("Room Name Field")]
        public InputField m_RoomNameInput;

        [Header("Create Room Button")]
        public Button m_CreateRoomButton;

        [Header("Room Name Field")]
        public Text m_GameMapText;
        public Text m_GameModesText;
        public Text m_PlayerLimitText;

        [Header("Game Map UI")]
        public Button m_NextGameMapButton;
        public Button m_PreviousGameMapButton;

        [Header("Game Mode UI")]
        public Button m_NextGameModeButton;
        public Button m_PreviousGameModeButton;

        [Header("Player Limit UI")]
        public Button m_NextPlayerLimitButton;
        public Button m_PreviousPlayerLimitButton;
        #endregion
    }
}