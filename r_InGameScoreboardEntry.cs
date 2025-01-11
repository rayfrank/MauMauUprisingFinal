using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

namespace ForceCodeFPS
{
    public class r_InGameScoreboardEntry : MonoBehaviour
    {
        #region Public variables
        [Header("Entry Information")]
        public Text m_PlayerName;
        public Text m_PlayerKills;
        public Text m_PlayerDeaths;
        public Text m_PlayerPing;
        #endregion

        #region Private variables
        [HideInInspector] public Player m_Player;
        #endregion

        #region Functions
        public void FixedUpdate()
        {
            this.m_PlayerKills.text = r_PlayerProperties.GetPlayerKills(this.m_Player).ToString();
            this.m_PlayerDeaths.text = r_PlayerProperties.GetPlayerDeaths(this.m_Player).ToString();

            this.m_PlayerPing.text = PhotonNetwork.GetPing().ToString();
        }
        #endregion

        #region Actions
        public void Initialize(Player _player)
        {
            //Set player
            this.m_Player = _player;

            //Set player name
            this.m_PlayerName.text = PhotonNetwork.LocalPlayer.NickName == _player.NickName ? this.m_Player.NickName + "   [YOU]" : this.m_Player.NickName;
        }
        #endregion
    }
}