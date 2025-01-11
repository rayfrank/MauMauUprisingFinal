using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;

namespace ForceCodeFPS
{
    #region Serializable Enums
    [System.Serializable] public enum r_GameModeType { FFA }
    #endregion

    #region Serializable Classes
    [System.Serializable]
    public class r_GameModeSetting
    {
        public string m_GameModeName;
        public r_GameModeType m_GameModeType;

        [Header("Settings")]
        public int m_MatchWaitingDuration;
        public int m_MatchPlayingDuration;
        public int m_MatchEndingDuration;

        [Header("Scores")]
        public int m_WinningKills;
    }
    #endregion

    public class r_InGameMode : MonoBehaviourPun
    {
        public static r_InGameMode Instance;

        #region Public variables
        [Header("Game Mode Settings")]
        public r_GameModeType m_GameMode;

        [Header("Game Mode Configurations")]
        public List<r_GameModeSetting> m_GameModes = new List<r_GameModeSetting>();
        #endregion

        #region Functions
        private void Awake()
        {
            if (Instance == null) Destroy(this.gameObject);

            Instance = this;
        }
        #endregion

        #region Get
        public r_GameModeSetting FindGameMode() => this.m_GameModes.Find(x => x.m_GameModeType == this.m_GameMode);
        #endregion
    }
}