using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;
using Photon.Realtime;

namespace ForceCodeFPS
{
    public class r_InGameScore : MonoBehaviourPunCallbacks
    {
        public static r_InGameScore Instance;

        #region Public Variables
        [Header("Score UI")]
        public Text m_WinningUserText;
        #endregion

        #region Private Variables
        [HideInInspector] public int m_WinningScore = 1;
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

        private void Start() => SetWinningScore(r_InGameMode.Instance.FindGameMode().m_WinningKills);
        #endregion

        #region Actions
        public void UpdateScore()
        {
            if (r_InGameMode.Instance.m_GameMode == r_GameModeType.FFA)
            {
                //Check winning score
                CheckWinningScoreFFA();
            }
        }

        private void CheckWinningScoreFFA()
        {
            foreach (Player _player in PhotonNetwork.PlayerList)
            {
                if (r_PlayerProperties.GetPlayerKills(_player) >= this.m_WinningScore)
                {
                    Debug.Log("Winner:" + _player.NickName);
                    Debug.Log("Kills: " + r_PlayerProperties.GetPlayerKills(_player));

                    //Update winner UI
                    this.m_WinningUserText.text = $"Winner: {_player.NickName}";

                    //Enable UI
                    this.m_WinningUserText.gameObject.SetActive(true);

                    //Change game state to ending
                    r_InGameManager.Instance.SetGameState(r_GameState.ENDING);
                }
            }
        }
        #endregion

        #region Set 
        private void SetWinningScore(int _winning_score) => this.m_WinningScore = _winning_score;
        #endregion

        #region Callbacks
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) => UpdateScore();
        #endregion
    }
}