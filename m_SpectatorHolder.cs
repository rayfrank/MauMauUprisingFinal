using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    public class m_SpectatorHolder : MonoBehaviour
    {
        public static m_SpectatorHolder instance;

        #region Public Variables
        [Header("Camera")]
        public Camera m_Camera;

        [Header("Spectator UI")]
        public GameObject m_SpectatorPanel;

        [Header("Spectator UI Content")]
        public Text m_KillerNameText;
        public Text m_KillerHealthText;
        public Text m_KillerWeaponText;
        public RawImage m_KillerWeaponImage;

        [Header("Spectator Settings")]
        public float m_SpectateTime = 10f;
        public float m_SpectateTimeReset = 10f;

        [Header("Spectator Camera Settings")]
        public float m_SpectateSpeed = 10f;
        #endregion

        #region Private Variables
        [Header("Target Transform")]
        [HideInInspector] public Transform m_Target;

        //Check spectating
        [HideInInspector] public bool m_Spectating;

        //Player controller Target
        [HideInInspector] public r_PlayerController m_TargetController;
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

            //Reset time
            m_SpectateTime = m_SpectateTimeReset;
        }

        private void FixedUpdate()
        {
            if (this.m_Target != null)
                HandleSpectating();

            else if (this.m_Target == null)
            {
                if (this.m_Spectating)
                    CancelSpectate();
                return;
            }
        }

        private void LateUpdate()
        {
            if (this.m_Target != null)
            {
                if (this.m_Spectating)
                    HandleSpectatingCamera();
            }
        }
        #endregion

        #region Handling
        private void HandleSpectating()
        {
            this.m_Spectating = this.m_SpectateTime > 0;

            if (this.m_Spectating)
            {
                this.m_SpectateTime -= Time.deltaTime;
                this.m_TargetController = this.m_Target.GetComponent<r_PlayerController>();
            }
            else
            {
                if (this.m_Camera.enabled == true)
                {
                    CancelSpectate();
                }
            }
        }

        private void HandleSpectatingCamera()
        {
            if (this.m_TargetController != null)
            {
                if (this.m_TargetController.m_ThirdPersonManager != null)
                {
                    Vector3 _spectatePosition = this.m_TargetController.m_ThirdPersonManager.m_SpectateHolder.position;

                    this.m_Camera.transform.position = _spectatePosition;

                    this.m_Camera.transform.LookAt(this.m_TargetController.m_PlayerCamera.m_CameraHolder);
                }
            }
        }
        #endregion

        #region Actions
        private void CancelSpectate()
        {
            //Disable camera
            m_Camera.enabled = false;

            //Reset target
            m_Target = null;

            //Reset spectate time
            m_SpectateTime = m_SpectateTimeReset;

            //Reset camera position
            m_Camera.transform.position = Vector3.zero;

            //Disable spectate panel
            SetUIPanel(this.m_SpectatorPanel, false);

            //Reset game settings
            r_InGameManager.Instance.ResetLocalGameSettings();

            //Disable spectate boolean
            this.m_Spectating = false;
        }

        public void UpdateUI(string _attacker, float _attackerHealth, string _attackerWeaponName)
        {
            this.m_KillerNameText.text = _attacker;
            this.m_KillerHealthText.text = _attackerHealth.ToString("000");
            this.m_KillerWeaponText.text = _attackerWeaponName;
            this.m_KillerWeaponImage.texture = this.m_Target.GetComponent<r_WeaponManager>().FindWeaponByName(_attackerWeaponName).m_WeaponData.m_WeaponTexture;
        }
        #endregion

        #region Set
        private void SetSpectate()
        {
            //Enable camera
            this.m_Camera.enabled = true;

            //Enable spectate panel
            SetUIPanel(this.m_SpectatorPanel, true);
        }

        public void SetTarget(string attacker, float _attackerHealth, string _attackerWeaponName)
        {
            if (GameObject.Find(attacker) != null)
            {
                //Find target
                this.m_Target = GameObject.Find(attacker).transform;
            }
            else
            {
                //Cancel spectate if enemy player is not found, reset local room settings
                CancelSpectate();

                return;
            }

            //Do spectate
            SetSpectate();

            //Update UI
            UpdateUI(attacker, _attackerHealth, _attackerWeaponName);

            //Set killer text
            SetUIText(this.m_KillerNameText, "Killed By " + attacker);
        }

        public void SetUIPanel(GameObject _panel, bool _state) => _panel.SetActive(_state);
        public void SetUIText(Text _text, string _string) => _text.text = _string;
        #endregion
    }
}