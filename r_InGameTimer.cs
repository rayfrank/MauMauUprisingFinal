using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ForceCodeFPS
{
    public class r_InGameTimer : MonoBehaviourPunCallbacks
    {
        public static r_InGameTimer instance;

        public const string RoomMatchTimerProperty = "StartTime";

        #region Public Variables
        [Header("Timer UI")]
        public Text m_TimerText;
        #endregion

        #region Private Variables
        //Local timer for each player
        [HideInInspector] public int m_StartedTimerValue;

        //Countdown Duration
        [HideInInspector] public int m_TimerDuration = 0;

        //Countdown State
        [HideInInspector] private bool m_TimerStarted;
        #endregion

        #region Functions
        private void Awake()
        {
            if (instance)
            {
                Destroy(instance);
                Destroy(instance.gameObject);
            }

            instance = this;
        }

        private void Update() => HandleTimer();
        #endregion

        #region Actions
        public void StartTimer(int _duration)
        {
            //Set duration
            this.m_TimerDuration = _duration;

            //Check if masterclient
            if (PhotonNetwork.IsMasterClient)
            {
                //Set timer property
                r_RoomProperties.WriteInt(PhotonNetwork.CurrentRoom, RoomMatchTimerProperty, (int)PhotonNetwork.ServerTimestamp);
            }

            //Initialize
            InitializeTimer();
        }

        public void InitializeTimer()
        {
            //Get started timer value
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomMatchTimerProperty, out object _start_time_propertie))
            {
                this.m_StartedTimerValue = (int)_start_time_propertie;

                //Set timer state
                this.m_TimerStarted = TimeRemaining() > 0;
            }
        }
        #endregion

        #region Handling
        private void HandleTimer()
        {
            if (!this.m_TimerStarted) return;

            //Calculate minutes and seconds
            int _minutes = ((int)TimeRemaining() / 60);
            int _seconds = ((int)TimeRemaining() % 60);

            //Set UI Text
            this.m_TimerText.text = _minutes.ToString("00") + ":" + _seconds.ToString("00");

            //Check timer
            if (TimeRemaining() <= 0.1f)
            {
                //Disable countdown
                this.m_TimerStarted = false;

                //Finished Countdown
                r_InGameManager.Instance.OnEndedCountdown();
            }
        }
        #endregion

        #region Get
        private float TimeRemaining() => this.m_TimerDuration - (PhotonNetwork.ServerTimestamp - this.m_StartedTimerValue) / 1000f;
        #endregion

        #region Callbacks
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) => InitializeTimer();
        #endregion
    }
}