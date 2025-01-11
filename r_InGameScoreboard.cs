using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

namespace ForceCodeFPS
{
    public class r_InGameScoreboard : MonoBehaviour
    {
        #region Public Variables
        [Header("Scoreboard Content")]
        public GameObject m_ScoreboardContent;

        [Header("Scoreboard Entry")]
        public GameObject m_ScoreboardEntry;
        #endregion

        #region Private Variables
        [HideInInspector] public List<r_InGameScoreboardEntry> m_Players = new List<r_InGameScoreboardEntry>();
        #endregion

        #region Functions
        private void FixedUpdate() => CheckPlayerList();
        #endregion

        #region Actions
        private void CheckPlayerList()
        {
            if (GetPlayerList().Count > 0)
            {
                for (int i = 0; i < GetPlayerList().Count; i++)
                {
                    if (GetEntryList().Find(x => x.m_Player == GetPlayerList()[i])) continue;

                    //Instantiate scoreboard entry
                    GameObject _entry = Instantiate(this.m_ScoreboardEntry, this.m_ScoreboardContent.transform);

                    //Find scoreboard entry script to setup
                    r_InGameScoreboardEntry _item = _entry.GetComponent<r_InGameScoreboardEntry>();
                    _item.Initialize(GetPlayerList()[i]);

                    //Add entry to player list
                    this.m_Players.Add(_item);
                }
            }

            if (GetEntryList().Count > 0)
            {
                for (int i = 0; i < GetEntryList().Count; i++)
                {
                    Player _found_player = GetPlayerList().Find(x => x == GetEntryList()[i].m_Player);

                    if (_found_player == null)
                    {
                        //Destroy entry
                        Destroy(GetEntryList()[i].gameObject);

                        //Remove player
                        m_Players.Remove(GetEntryList()[i]);
                    }
                }
            }

            SortScoreboard();
        }

        public void SortScoreboard()
        {
            List<r_InGameScoreboardEntry> _sortedPlayerList = this.m_Players.OrderByDescending(x => x.m_Player.CustomProperties[r_PlayerProperties.KillsPropertyKey]).ToList();

            for (int i = 0; i < _sortedPlayerList.Count; i++)
            {
                _sortedPlayerList[i].transform.SetSiblingIndex(i);
            }
        }
        #endregion

        #region Get
        private List<Player> GetPlayerList() { return PhotonNetwork.PlayerList.ToList(); }
        private List<r_InGameScoreboardEntry> GetEntryList() { return this.m_Players; }
        #endregion
    }
}