using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ForceCodeFPS
{
    public class r_PhotonHandler : MonoBehaviourPunCallbacks
    {
        public static r_PhotonHandler instance;

        #region Functions
        private void Awake()
        {
            if (instance)
            {
                Destroy(instance);
                Destroy(instance.gameObject);
            }

            instance = this;
            PhotonNetwork.IsMessageQueueRunning = false;
        }

        private void Start()
        {
            DontDestroyOnLoad(this);

            PhotonNetwork.SendRate = 20;
            PhotonNetwork.SerializationRate = 5;

            PhotonNetwork.AutomaticallySyncScene = true;

            if (!PhotonNetwork.IsConnected)
                r_PhotonHandler.instance.ConnectToPhoton();
        }
        #endregion

        #region Actions
        public void ConnectToPhoton() => PhotonNetwork.ConnectUsingSettings();

        public void LoadGame()
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(PhotonNetwork.CurrentRoom.CustomProperties["GameMap"].ToString() + "_" + PhotonNetwork.CurrentRoom.CustomProperties["GameMode"].ToString());
        }
        #endregion

        #region Callbacks
        public override void OnConnected() => Debug.Log("Connected to Photon");

        public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby(TypedLobby.Default);

        public void CreateRoom(string _RoomName, RoomOptions _RoomOptions) => PhotonNetwork.CreateRoom(_RoomName, _RoomOptions, null, null);

        public override void OnCreatedRoom() => Debug.Log("Created Room");

        public override void OnJoinedLobby() => Debug.Log("Connected to Lobby");

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            if (r_RoomBrowserController.instance != null)
            {
                r_RoomBrowserController.instance.m_RoomBrowserList = roomList;
                r_RoomBrowserController.instance.RefreshRoomBrowser();
            }

            r_LobbyController.instance.ReceiveRoomList(roomList);
        }

        public override void OnJoinedRoom()
        {
            PhotonNetwork.IsMessageQueueRunning = true;

            string _RoomState = (string)PhotonNetwork.CurrentRoom.CustomProperties["RoomState"];

            switch (_RoomState)
            {
                case "InLobby": r_LobbyController.instance.EnterLobby(); break;
                case "InGame": LoadGame(); break;
            }
        }

        public override void OnLeftRoom()
        {
            //Resest player properties
            r_PlayerProperties.WriteInt(PhotonNetwork.LocalPlayer, r_PlayerProperties.KillsPropertyKey, 0);
            r_PlayerProperties.WriteInt(PhotonNetwork.LocalPlayer, r_PlayerProperties.DeathsPropertyKey, 0);

            //Load main menu scene
            if (SceneManager.GetActiveScene().buildIndex != 0) PhotonNetwork.LoadLevel(0);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            string _RoomState = (string)PhotonNetwork.CurrentRoom.CustomProperties["RoomState"];

            switch (_RoomState)
            {
                case "InLobby": r_LobbyController.instance.ListLobbyPlayers(); break;
                case "InGame":

                    if (PhotonNetwork.IsMasterClient)
                        PhotonNetwork.RemoveRPCs(otherPlayer);

                    break;
            }
        }
        #endregion
    }
}