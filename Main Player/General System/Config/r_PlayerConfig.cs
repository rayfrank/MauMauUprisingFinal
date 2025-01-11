using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

namespace ForceCodeFPS
{
    public class r_PlayerConfig : MonoBehaviourPun
    {
        #region References
        public r_PlayerController m_PlayerController
        {
            get => this.transform.GetComponent<r_PlayerController>();
            set => this.m_PlayerController = value;
        }

        public r_WeaponManager m_WeaponManager
        {
            get => this.m_PlayerController != null ? this.m_PlayerController.m_WeaponManager : this.transform.GetComponent<r_WeaponManager>();
            set => this.m_WeaponManager = value;
        }
        #endregion

        #region Public Variables
        [Header("Local Player Configuration")]
        public MonoBehaviour[] m_LocalScripts;
        public GameObject[] m_LocalObjects;
        #endregion

        #region Actions
        public void SetupLocalPlayer(int[] _loadout_weapon_ids)
        {
            if (photonView.IsMine)
            {
                //Enable local objects and scripts
                if (this.m_LocalScripts.Length > 0) foreach (MonoBehaviour _component in this.m_LocalScripts) _component.enabled = true;
                if (this.m_LocalObjects.Length > 0) foreach (GameObject _object in this.m_LocalObjects) _object.SetActive(true);

                //Set name
                photonView.RPC(nameof(SetPlayerName), RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.NickName);

                //Save loadout 
                this.m_WeaponManager.OnLoadoutSelect(_loadout_weapon_ids);
            }
        }
        #endregion

        #region Set
        [PunRPC]
        private void SetPlayerName(string _name) => this.gameObject.name = _name;
        #endregion
    }
}