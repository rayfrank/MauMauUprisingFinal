using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;
using Photon.Realtime;

namespace ForceCodeFPS
{
    public class r_KillfeedManager : MonoBehaviour
    {
        public static r_KillfeedManager instance;

        #region Public Variables
        [Header("Photonview")]
        public PhotonView m_Photonview;

        [Header("UI")]
        public Transform m_KillfeedContent;
        public GameObject m_KillfeedPrefab;

        [Header("Killfeed UI Settings")]
        public Color m_UsernameColor;
        public Color m_DefaultTextColor;

        [Header("Killfeed Settings")]
        public float m_KillfeedDuration = 5f;
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
        #endregion

        #region Actions
        public void AddKillfeed(Player _killer, string _weaponName, Player _eliminated) => this.m_Photonview.RPC(nameof(AddKillfeedRPC), RpcTarget.All, _killer, _weaponName, _eliminated);
        #endregion

        #region Network Events
        [PunRPC]
        public void AddKillfeedRPC(Player _killer, string _weaponName, Player _eliminated)
        {
            //Instantiate killfeed
            GameObject _killfeed = Instantiate(this.m_KillfeedPrefab, this.m_KillfeedContent);

            //Player name color
            Color _killer_text_color = PhotonNetwork.LocalPlayer.NickName == _killer.NickName ? this.m_UsernameColor : this.m_DefaultTextColor;
            Color _eliminated_text_color = PhotonNetwork.LocalPlayer.NickName == _eliminated.NickName ? this.m_UsernameColor : this.m_DefaultTextColor;

            //Change color to RGBA
            string _killer_color_RGBA = ColorUtility.ToHtmlStringRGBA(_killer_text_color);
            string _eliminated_color_RGBA = ColorUtility.ToHtmlStringRGBA(_eliminated_text_color);

            //Set text
            _killfeed.GetComponent<Text>().text = $"<color=#{_killer_color_RGBA}>{_killer.NickName}</color> [{_weaponName}] <color=#{_eliminated_color_RGBA}>{_eliminated.NickName}</color>";

            //Destroy killfeed
            Destroy(_killfeed, this.m_KillfeedDuration);
        }
        #endregion
    }
}