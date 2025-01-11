using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using Photon.Realtime;

namespace ForceCodeFPS
{
    #region Serializable classes
    [System.Serializable]
    public class r_AimPart
    {
        [Header("Aim Tansform")]
        public Transform m_AimTransform;

        [Header("Weight")]
        [Range(0, 1)] public float m_Weight;
    }
    #endregion

    public class r_ThirdPersonAimIK : MonoBehaviourPunCallbacks
    {
        #region Public Variables
        [Header("Player Controller")]
        public r_PlayerController m_PlayerController;

        [Header("Related Transform")]
        public Transform m_TargetTransform;
        public Transform m_AimTransform;

        [Header("Aim Settings")]
        public float m_AimSmoothness;

        [Header("Aim Configuration")]
        public List<r_AimPart> m_AimParts = new List<r_AimPart>();
        #endregion

        #region Private Variables
        //Weights
        private float[] m_Weights;
        #endregion

        #region Functions
        private void Start() => SaveWeights();

        private void Update() => HandleWeights();

        private void LateUpdate()
        {
            if (this.m_AimParts.Count > 0 && this.m_TargetTransform != null && this.m_AimTransform != null) HandleAimIK();
        }
        #endregion

        #region Handling
        private void HandleWeights()
        {
            for (int i = 0; i < this.m_Weights.Length; i++)
            {
                if (this.m_PlayerController.m_MoveState == r_MoveState.SPRINTING)
                {
                    if (this.m_AimParts[i].m_Weight != 0)
                        this.m_AimParts[i].m_Weight = Mathf.Lerp(this.m_AimParts[i].m_Weight, 0, Time.deltaTime * this.m_AimSmoothness);
                }
                else
                {
                    if (this.m_AimParts[i].m_Weight != this.m_Weights[i])
                        AddWeight(i, this.m_Weights[i]);
                }
            }
        }

        private void HandleAimIK()
        {
            foreach (r_AimPart _AimPart in this.m_AimParts)
            {
                if (_AimPart.m_AimTransform)
                {
                    //Handle IK aiming
                    FaceTarget(_AimPart.m_AimTransform, _AimPart.m_Weight);
                }
            }
        }
        #endregion

        #region Actions
        private void FaceTarget(Transform _aim_part_transform, float _Weight)
        {
            //Save aim direction
            Vector3 _aim_direction = this.m_AimTransform.forward;
            Vector3 _target_direction = this.m_TargetTransform.position - _aim_part_transform.position;

            //Declare towards rotation
            Quaternion _aim_towards = Quaternion.FromToRotation(_aim_direction, _target_direction);

            //Slerp aim rotation
            Quaternion _final_rotation = Quaternion.Slerp(Quaternion.identity, _aim_towards, _Weight);

            //Apply aim rotation
            _aim_part_transform.rotation = _final_rotation * _aim_part_transform.rotation;
        }
        #endregion

        #region Set
        private void SaveWeights()
        {
            this.m_Weights = new float[this.m_AimParts.Count];

            for (int i = 0; i < this.m_AimParts.Count; i++)
                this.m_Weights[i] = this.m_AimParts[i].m_Weight;
        }

        private void AddWeight(int _ID, float _Weight) => m_AimParts[_ID].m_Weight = Mathf.Lerp(m_AimParts[_ID].m_Weight, _Weight, Time.deltaTime * this.m_AimSmoothness);
        #endregion
    }
}