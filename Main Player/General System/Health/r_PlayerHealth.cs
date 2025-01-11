using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace ForceCodeFPS
{
    public class r_PlayerHealth : MonoBehaviourPun
    {
        #region References
        public r_PlayerController m_PlayerController;
        #endregion

        #region Public variables
        [Header("Health Base Configuration")]
        public r_PlayerHealthBase m_HealthBase;
        #endregion

        #region Private variables
        //Current health
        [HideInInspector] public float m_Health;

        //Dead tracker
        [HideInInspector] public bool m_IsDeath;

        //Last attacker information
        [HideInInspector] public string m_LastAttackerName;
        [HideInInspector] public float m_LastAttackerHealth;
        [HideInInspector] public string m_LastAttackerWeapon;
        #endregion

        #region Functions
        private void Start() => SetDefaults();
        #endregion

        #region Actions
        public void DecreaseHealth(string _senderName, float _Amount, Vector3 _senderPosition, float _senderHealth, string _senderWeaponName) => photonView.RPC(nameof(DecreaseHealthRPC), RpcTarget.AllBuffered, _senderName, _Amount, _senderPosition, _senderHealth, _senderWeaponName);
        public void IncreaseHealth(float _Amount) => photonView.RPC(nameof(IncreaseHealthRPC), RpcTarget.AllBuffered, _Amount);
        #endregion

        #region Set
        private void SetDefaults()
        {
            //Increase health
            IncreaseHealth(this.m_HealthBase.m_MaxHealth);

            //Reset death boolean
            this.m_IsDeath = false;
        }
        #endregion

        #region Network Events
        [PunRPC]
        private void DecreaseHealthRPC(string _senderName, float _Amount, Vector3 _senderPosition, float _senderHealth, string _senderWeaponName)
        {
            //Save attacker data to use in spectator
            this.m_LastAttackerName = _senderName;
            this.m_LastAttackerHealth = _senderHealth;
            this.m_LastAttackerWeapon = _senderWeaponName;

            //Decrease our current health
            this.m_Health -= _Amount;

            //set health text UI
            this.m_PlayerController.m_PlayerUI.SetHealthText(this.m_Health);

            //Set bloody screen UI
            this.m_PlayerController.m_PlayerUI.SetBloodyScreen();

            //Avoid camera hit effect if the player is damaged by local player
            if (_senderName != PhotonNetwork.LocalPlayer.NickName)
            {
                //Set camera hit reaction effect
                this.m_PlayerController.m_PlayerCamera.OnCameraHit(_senderPosition);
            }

            if (photonView.IsMine)
            {
                //Set damage indicator
                this.m_PlayerController.m_PlayerUI.SetDamageIndicator(_senderName, _senderPosition);

                //Play hurt audio
                this.m_PlayerController.m_PlayerAudio.OnPlayerHurtAudioPlay(this.gameObject.transform.position);
            }

            //If our current health less is then 0, then player die
            if (this.m_Health <= 0)
            {
                //Die function
                Suicide();
            }
        }

        [PunRPC]
        private void IncreaseHealthRPC(float _Amount)
        {
            //If health is full, then return
            if (this.m_Health >= this.m_HealthBase.m_MaxHealth) return;

            if (_Amount >= this.m_HealthBase.m_MaxHealth - this.m_Health)
            {
                //Calculate and add health amount how much we need to full (If the amount is to much)
                this.m_Health += this.m_HealthBase.m_MaxHealth - this.m_Health;
            }
            else if (_Amount < this.m_HealthBase.m_MaxHealth - this.m_Health)
            {
                //If the health is not full, and we can easy add the health
                this.m_Health += _Amount;
            }

            //set health text UI
            this.m_PlayerController.m_PlayerUI.SetHealthText(this.m_Health);
        }

        private void Suicide()
        {
            //Set our current health on 0
            this.m_Health = 0;

            //Set death
            this.m_IsDeath = true;

            //Drop all player weapons
            this.m_PlayerController.m_WeaponManager.OnDropAllWeapons();

            if (photonView.IsMine)
            {
                //Set death
                r_PlayerProperties.WriteInt(PhotonNetwork.LocalPlayer, r_PlayerProperties.DeathsPropertyKey, (int)r_PlayerProperties.GetPlayerDeaths(PhotonNetwork.LocalPlayer) + 1);

                //Enable death camera attached to ragdoll
                this.m_PlayerController.m_ThirdPersonManager.m_ThirdPersonCamera.SetDeathCamera(this.m_LastAttackerName, this.transform.name, this.m_LastAttackerHealth, this.m_LastAttackerWeapon);
            }

            //Set ragdoll parent to null
            this.m_PlayerController.m_ThirdPersonManager.transform.parent = null;

            //Third Person 
            this.m_PlayerController.m_ThirdPersonManager.ThirdPersonSuicide();

            if (photonView.IsMine)
            {
                //Destroy our player object
                PhotonNetwork.Destroy(this.gameObject);
            }
        }
        #endregion
    }
}