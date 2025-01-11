using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForceCodeFPS
{
    public class r_PlayerCamera : MonoBehaviour
    {
        #region References
        public r_PlayerController m_PlayerController
        {
            get => this.transform.gameObject.GetComponent<r_PlayerController>();
            set => this.m_PlayerController = value;
        }
        #endregion

        #region Public variables
        [Header("Camera Base Configuration")]
        public r_PlayerCameraBase m_CameraBase;

        [Header("Player Camera")]
        public Camera m_PlayerCamera;

        [Header("Player Camera Holder")]
        public Transform m_CameraHolder;

        [Header("Camera Movement Holder")]
        public Transform m_HitReactionHolder;
        public Transform m_HeadbobHolder;
        public Transform m_JumpLandHolder;
        public Transform m_LeanPivotHolder;
        public Transform m_RecoilHolder;
        #endregion

        #region Private variables
        //Current camera state
        [HideInInspector] public r_CameraState m_CameraState;

        //Current mouse input data
        [HideInInspector] public float m_MouseX;
        [HideInInspector] public float m_MouseY;

        //Current mouse look sensitivity
        [HideInInspector] public float m_MouseSensitivity;

        //Current headbob timer
        [HideInInspector] public float m_HeadbobTimer;

        //Weapon aim data
        [HideInInspector] public float m_WeaponCameraFOV;
        [HideInInspector] public float m_WeaponCameraFOVSpeed;

        //Check camera jumping/landing effect
        [HideInInspector] public bool m_IsHandlingCameraFallEffect;

        //Current camera lean angle
        [HideInInspector] public float m_CameraRotationLeanAngle;
        #endregion

        #region Functions
        private void Update()
        {
            if (this.m_CameraHolder != null)
            {
                HandleCamera();
                HandleCameraStates();
            }

            if (this.m_HeadbobHolder != null) HandleHeadbob();
            if (this.m_HitReactionHolder != null) HandleCameraHitReaction();
            if (this.m_JumpLandHolder != null && this.m_CameraBase.m_CameraJumpLandSettings.m_jumpLandFunction) HandleCameraJumpLand();
            if (this.m_LeanPivotHolder != null && this.m_CameraBase.m_CameraLeanSettings.m_LeanFeature) HandleCameraLeaning();
        }
        #endregion

        #region Handling
        private void HandleCamera()
        {
            //Check mouse sensitivity
            this.m_MouseSensitivity = this.m_CameraState == r_CameraState.AIMING ? this.m_CameraBase.m_CameraGeneralSettings.m_aimSensitivity : this.m_CameraBase.m_CameraGeneralSettings.m_hipSensitivity;

            //Handle mouse inputs X and Y and apply our sensitivity
            this.m_MouseX += this.m_PlayerController.m_InputManager.GetMouseX() * this.m_MouseSensitivity;
            this.m_MouseY += this.m_PlayerController.m_InputManager.GetMouseY() * this.m_MouseSensitivity;

            //Mouse X rotation and clamping mouse Y
            this.m_MouseX %= 360f;
            this.m_MouseY = Mathf.Clamp(this.m_MouseY, -this.m_CameraBase.m_CameraGeneralSettings.m_MinMouseY, this.m_CameraBase.m_CameraGeneralSettings.m_MaxMouseY);

            //Rotate the player transform rotation with mouse X and the camera holder with mouse Y
            this.m_PlayerController.transform.rotation = Quaternion.Euler(0f, this.m_MouseX, 0f);
            this.m_CameraHolder.localRotation = Quaternion.Euler(-this.m_MouseY, 0, 0f);
        }

        private void HandleCameraStates()
        {
            //Set camera state hip or aiming based on weapon aiming
            switch (this.m_CameraState)
            {
                //Handle camera field of view on not aiming
                case r_CameraState.HIP: if (this.m_PlayerCamera.fieldOfView != GetMoveCameraState().m_CameraFOV) HandleMovementCameraFOV(); break;

                //Handle camera field of view on aiming
                case r_CameraState.AIMING: HandleAimingFieldOfView(this.m_CameraState); break;
            }
        }

        private void HandleHeadbob()
        {
            //Check if we are moving to handle headbobbing
            if (Mathf.Abs(this.m_PlayerController.m_moveDirection.x) > 0.1f || Mathf.Abs(this.m_PlayerController.m_moveDirection.z) > 0.1f)
            {
                //Return if we are not grounded
                if (!this.m_PlayerController.m_CharacterController.isGrounded) return;

                //Increase headbob timer while we are moving
                this.m_HeadbobTimer += Time.deltaTime * GetMoveCameraState().m_HeadbobSpeed;

                //Calculate our desired headbob position
                Vector3 _DesiredHeadbob = new Vector3(Mathf.Cos(this.m_HeadbobTimer / 2) * GetMoveCameraState().m_HeadbobAmount * -1, Mathf.Sin(this.m_HeadbobTimer) * GetMoveCameraState().m_HeadbobAmount, this.m_HeadbobHolder.localPosition.x);

                //Lerp our headbob holder position to our desired headbob position
                this.m_HeadbobHolder.localPosition = Vector3.Lerp(this.m_HeadbobHolder.localPosition, _DesiredHeadbob, Time.deltaTime * 8f);
            }
            else
            {
                //If we are not moving, return our headbob holder position to zero
                if (this.m_HeadbobHolder.localPosition != Vector3.zero)
                    this.m_HeadbobHolder.localPosition = Vector3.Lerp(this.m_HeadbobHolder.localPosition, Vector3.zero, Time.deltaTime * 8f);
                else return;
            }
        }

        private void HandleCameraJumpLand()
        {
            //Return camera jump land effect position and rotation
            this.m_JumpLandHolder.localPosition = Vector3.Lerp(this.m_JumpLandHolder.localPosition, Vector3.zero, Time.deltaTime * this.m_CameraBase.m_CameraJumpLandSettings.m_effectDurationReturn);
            this.m_JumpLandHolder.localRotation = Quaternion.Slerp(this.m_JumpLandHolder.localRotation, Quaternion.identity, Time.deltaTime * this.m_CameraBase.m_CameraJumpLandSettings.m_effectDurationReturn);
        }

        private void HandleCameraHitReaction()
        {
            //Return camera hit reaction position to zero
            this.m_HitReactionHolder.localRotation = Quaternion.Lerp(this.m_HitReactionHolder.localRotation, Quaternion.identity, Time.deltaTime * this.m_CameraBase.m_CameraHitReactionSettings.m_CameraHitDamping);
        }

        private void HandleCameraLeaning()
        {
            if (this.m_PlayerController.m_InputManager.GetLeanLeftKey() && this.m_PlayerController.m_MoveState != r_MoveState.SPRINTING)
            {
                //Move lean angle to left
                this.m_CameraRotationLeanAngle = Mathf.MoveTowardsAngle(this.m_CameraRotationLeanAngle, this.m_CameraBase.m_CameraLeanSettings.m_LeanRotationAngle, this.m_CameraBase.m_CameraLeanSettings.m_LeanAngleSpeed * Time.deltaTime);

                //Set camera lean position
                ChangeCameraLeanPosition(-this.m_CameraBase.m_CameraLeanSettings.m_LeanMoveAngle);
            }
            else if (this.m_PlayerController.m_InputManager.GetLeanRightKey() && this.m_PlayerController.m_MoveState != r_MoveState.SPRINTING)
            {
                //Move lean angle to right
                this.m_CameraRotationLeanAngle = Mathf.MoveTowardsAngle(this.m_CameraRotationLeanAngle, -this.m_CameraBase.m_CameraLeanSettings.m_LeanRotationAngle, this.m_CameraBase.m_CameraLeanSettings.m_LeanAngleSpeed * Time.deltaTime);

                //Set camera lean position
                ChangeCameraLeanPosition(this.m_CameraBase.m_CameraLeanSettings.m_LeanMoveAngle);
            }
            else
            {
                //Move lean angle to zero
                this.m_CameraRotationLeanAngle = Mathf.MoveTowardsAngle(this.m_CameraRotationLeanAngle, 0, this.m_CameraBase.m_CameraLeanSettings.m_LeanAngleSpeed * Time.deltaTime);

                //Set camera lean position to zero
                ChangeCameraLeanPosition(0);
            }

            //Set lean rotation
            this.m_LeanPivotHolder.localRotation = Quaternion.AngleAxis(this.m_CameraRotationLeanAngle, Vector3.forward);
        }

        private void HandleAimingFieldOfView(r_CameraState _cameraState)
        {
            //Check weapon FOV
            float _cameraFOV = _cameraState == r_CameraState.AIMING ? this.m_WeaponCameraFOV : this.m_CameraBase.m_CameraGeneralSettings.m_cameraFOV;

            //set camera weapon FOV
            this.m_PlayerCamera.fieldOfView = Mathf.Lerp(this.m_PlayerCamera.fieldOfView, _cameraFOV, Time.deltaTime * this.m_WeaponCameraFOVSpeed);
        }

        private void HandleMovementCameraFOV() => this.m_PlayerCamera.fieldOfView = Mathf.Lerp(this.m_PlayerCamera.fieldOfView, this.GetMoveCameraState().m_CameraFOV, Time.deltaTime * this.m_CameraBase.m_CameraGeneralSettings.m_CameraFOVSpeed);
        #endregion

        #region Actions
        private void ChangeCameraLeanPosition(float _lean_value)
        {
            //Declare desired lean position
            Vector3 _desired_lean_position = new Vector3(_lean_value, this.m_LeanPivotHolder.localPosition.y, this.m_LeanPivotHolder.localPosition.z);

            //Lerp to lean position
            this.m_LeanPivotHolder.localPosition = Vector3.Lerp(this.m_LeanPivotHolder.localPosition, _desired_lean_position, this.m_CameraBase.m_CameraLeanSettings.m_LeanRate * Time.deltaTime);
        }

        public void OnCameraHit(Vector3 _hit_direction)
        {
            //Calculate cross
            Vector3 _cross = this.m_HitReactionHolder.InverseTransformDirection(Vector3.Cross(this.m_HitReactionHolder.forward, this.m_PlayerController.transform.position - _hit_direction));

            //Calculate direction
            Quaternion _direction = this.m_HitReactionHolder.localRotation * Quaternion.Euler(_cross.normalized * this.m_CameraBase.m_CameraHitReactionSettings.m_CameraHitForce);

            //Trigger camera hit effect
            StartCoroutine(OnCameraHitEffect(_direction));
        }

        private IEnumerator OnCameraHitEffect(Quaternion _hit_direction)
        {
            //Declare timer
            float _time = 0.0f;

            while (_time < this.m_CameraBase.m_CameraHitReactionSettings.m_CameraHitDuration)
            {
                //Increase time with delta time
                _time += Time.deltaTime;

                // Move the camera towards the target position
                this.m_HitReactionHolder.localRotation = Quaternion.Lerp(this.m_HitReactionHolder.localRotation, _hit_direction, _time);

                yield return null;
            }
        }

        public void onCameraFall(bool _onJump)
        {
            //Start corountine camera fall effect if the function is enabled
            if (this.m_CameraBase.m_CameraJumpLandSettings.m_jumpLandFunction)
            {
                if (!this.m_IsHandlingCameraFallEffect)
                    StartCoroutine(OnCameraFallEffect(_onJump));
            }
        }

        private IEnumerator OnCameraFallEffect(bool _onJump)
        {
            this.m_IsHandlingCameraFallEffect = true;

            //Start time on zero
            float _time = 0.0f;

            //Declare start position and rotation
            Vector3 _startPosition = this.m_JumpLandHolder.localPosition;
            Quaternion _startRotation = this.m_JumpLandHolder.localRotation;

            while (_time < this.m_CameraBase.m_CameraJumpLandSettings.m_effectDuration)
            {
                //Increase time with delta time
                _time += Time.deltaTime;

                //Check jump or land position and rotation
                Vector3 _endPosition = _onJump ? _startPosition + this.m_CameraBase.m_CameraJumpLandSettings.m_jumpPosition : _startPosition + this.m_CameraBase.m_CameraJumpLandSettings.m_landPosition;
                Quaternion _endRotation = _onJump ? _startRotation * Quaternion.Euler(this.m_CameraBase.m_CameraJumpLandSettings.m_jumpRotation) : _startRotation * Quaternion.Euler(this.m_CameraBase.m_CameraJumpLandSettings.m_landRotation);

                //Apply position and rotation
                this.m_JumpLandHolder.localPosition = Vector3.Lerp(this.m_JumpLandHolder.localPosition, _endPosition, _time);
                this.m_JumpLandHolder.localRotation = Quaternion.Slerp(this.m_JumpLandHolder.localRotation, _endRotation, _time);

                yield return null;
            }

            this.m_IsHandlingCameraFallEffect = false;
        }
        #endregion

        #region Get
        public r_MoveCameraSetting GetMoveCameraState() => this.m_CameraBase.m_MoveCameraSettings.Find(x => x.m_MoveState == this.m_PlayerController.m_MoveState);
        #endregion
    }
}