using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace ForceCodeFPS
{
    public class r_PlayerController : MonoBehaviourPun
    {
        #region References
        [Header("Character Controller")]
        public CharacterController m_CharacterController;

        [Header("Player References")]
        public r_PlayerCamera m_PlayerCamera;
        public r_PlayerHealth m_PlayerHealth;
        public r_PlayerAudio m_PlayerAudio;

        [Header("Weapon References")]
        public r_WeaponManager m_WeaponManager;

        [Header("Third Person References")]
        public r_ThirdPersonManager m_ThirdPersonManager;

        [Header("Networking")]
        public PhotonView m_PhotonView;

        public r_InputManager m_InputManager;
        #endregion

        #region Public variables
        [Header("Player Base Configuration")]
        public r_PlayerControllerBase m_ControllerBase;

        [Header("Player HUD")]
        public r_PlayerUI m_PlayerUI;

        [Header("Current move state")]
        public r_MoveState m_MoveState;
        #endregion

        #region Private variables
        //Input direction
        [HideInInspector] public Vector3 m_InputMovement;

        //move direction
        [HideInInspector] public Vector3 m_moveDirection;
        [HideInInspector] public Vector3 m_moveDirectionVelocityRef;

        //current speed based on move states
        [HideInInspector] public float m_CurrentSpeed;

        //Grounded and previous grounded for land/jump
        [HideInInspector] public bool m_grounded;
        [HideInInspector] public bool m_previousGrounded;

        //Stamina
        [HideInInspector] public float m_stamina;
        [HideInInspector] public bool m_canUseStamina;

        //Falling data for falldamage
        [HideInInspector] public float m_startFallPositionY;
        [HideInInspector] public float m_endFallPositionY;

        //Sliding data
        [HideInInspector] public float m_slideTime;
        [HideInInspector] public Vector3 m_slideDirection;

        //Jump data
        [HideInInspector] public int m_JumpCount;

        //Walljump data
        [HideInInspector] public int m_WalljumpCount;

        //Jumppad data
        [HideInInspector] public float m_JumppadForce;
        [HideInInspector] public bool m_EnteredJumppad;

        //Raycast used for slopes
        [HideInInspector] public RaycastHit m_raycastHit;
        #endregion

        #region Functions
        private void Start() => SetDefaults();

        private void Update()
        {
            HandleMovement();
            HandleMovementStates();
            HandleMovementStatesAudio();

            if (this.m_ControllerBase.m_staminaFeature) HandleStamina();
        }
        #endregion

        #region Handling
        private void HandleMovement()
        {
            //Handle on land and jump effect
            if (!this.m_previousGrounded && this.m_grounded) OnLand();
            if (this.m_previousGrounded && !this.m_grounded) this.m_startFallPositionY = this.transform.position.y;

            //Set grounded to previous grounded to make land / jump effect
            this.m_previousGrounded = this.m_grounded;

            //Movement on grounded
            if (this.m_CharacterController.isGrounded)
            {
                //Handle horizontal and vertical movement
                if (this.m_MoveState != r_MoveState.SLIDING)
                {
                    //Set horizontal and vertical input
                    this.m_InputMovement = (GetNormalizedDirection() * this.m_InputManager.GetVertical() + this.transform.right * this.m_InputManager.GetHorizontal()).normalized;

                    //Check desired movement
                    Vector3 _desired_movement = (OnMaxSlope() ? GetSlopeSlideDirection() * this.m_ControllerBase.m_slopeSlideSpeed : this.m_InputMovement * this.m_CurrentSpeed);

                    //Apply slope direction to move direction on slope, otherwise use straight inputMovement and set speed
                    this.m_moveDirection = Vector3.SmoothDamp(this.m_moveDirection, _desired_movement, ref this.m_moveDirectionVelocityRef, this.m_ControllerBase.m_AccelerationSpeed);
                }

                //Stick to ground force
                this.m_moveDirection.y = -this.m_ControllerBase.m_StickToGroundForce;

                //Check and set jumping
                HandleJumping();

                //Check and set crouching
                HandleCrouching(this.m_MoveState == r_MoveState.CROUCHING || this.m_MoveState == r_MoveState.SLIDING);

                //Handle Sliding
                if (this.m_ControllerBase.m_slideFeature) HandleSliding();

                //Handle jumppad
                if (this.m_EnteredJumppad) SetJump(m_JumppadForce);
            }
            else
            {
                //Disable jumppad
                this.m_EnteredJumppad = false;

                if (this.m_JumpCount < this.m_ControllerBase.m_MaxJumpCount)
                {
                    if (this.m_InputManager.GetJump())
                    {
                        //Set jump
                        SetJump(this.m_ControllerBase.m_JumpHeight);
                    }
                }

                //Apply gravity to movement
                this.m_moveDirection += Physics.gravity * Time.deltaTime * this.m_ControllerBase.m_Gravity;
            }

            //Check if colliders below to grounded and make the player move
            this.m_grounded = (this.m_CharacterController.Move((this.m_moveDirection) * Time.deltaTime) & CollisionFlags.Below) != 0;
        }

        private void HandleMovementStates()
        {
            //Find current speed based on current movement state
            this.m_CurrentSpeed = this.m_ControllerBase.m_MoveStateSettings.FirstOrDefault(x => x.m_MoveState == this.m_MoveState).m_MoveSpeed;

            //Check sliding
            if (this.m_MoveState == r_MoveState.SLIDING) return;

            //Set walking if magnitude of our move direction is greater then a little value
            if (this.m_moveDirection.magnitude > 0.2f && this.m_MoveState != r_MoveState.CROUCHING)
            {
                //Change current state to walking
                SetMoveState(r_MoveState.WALKING);
            }

            //Make sprinting only possible if the player is going forward and grounded
            if (this.m_InputManager.GetSprint() && this.m_InputManager.GetVertical() > 0.5f && this.m_CharacterController.isGrounded && CanStandup() && m_canUseStamina)
            {
                //Change current state to sprinting
                SetMoveState(r_MoveState.SPRINTING);
            }

            //If the player is crouched and try to crouch, stand up
            if (this.m_MoveState == r_MoveState.CROUCHING && this.m_InputManager.GetCrouch() && CanStandup())
            {
                //Change current state to idle for standing up
                SetMoveState(r_MoveState.IDLE);
            }
            else if (this.m_MoveState != r_MoveState.CROUCHING && !this.m_InputManager.GetSprint() && this.m_InputManager.GetCrouch())
            {
                //Change current state to crouching
                SetMoveState(r_MoveState.CROUCHING);
            }

            //If the player horizontal and vertical input is zero, set to idle. Check if player isn't crouched, otherwise can't crouch and will update idle..
            if (this.m_InputManager.GetHorizontal() == 0 && this.m_InputManager.GetVertical() == 0 && this.m_MoveState != r_MoveState.CROUCHING)
            {
                //Change current state to idle
                SetMoveState(r_MoveState.IDLE);
            }

            //When the player is in air
            if (!this.m_grounded)
            {
                //Change current state to jumping
                SetMoveState(r_MoveState.JUMPING);
            }
        }

        private void HandleJumping()
        {
            if (this.m_InputManager.GetJump() && CanStandup())
            {
                //Check if the player is crouching otherwise jump straight
                if (this.m_MoveState == r_MoveState.CROUCHING)
                {
                    //If trying to jump while crouching, stand up
                    SetMoveState(r_MoveState.IDLE);
                }
                else
                {
                    //Stop sliding on jump
                    if (this.m_MoveState == r_MoveState.SLIDING)
                    {
                        if (this.m_slideTime < this.m_ControllerBase.m_CanSlideJumpLength)
                        {
                            //Cancel slide
                            SetSlide(false);
                        }
                    }

                    if (this.m_MoveState != r_MoveState.SLIDING) SetJump(this.m_ControllerBase.m_JumpHeight);
                }
            }
        }

        private void HandleCrouching(bool _crouching)
        {
            //Change Character Controller Height
            this.m_CharacterController.height = _crouching ? this.m_ControllerBase.m_CrouchHeight : this.m_ControllerBase.m_StandHeight;

            //Change Character Controller center height
            this.m_CharacterController.center = _crouching ? new Vector3(0f, this.m_ControllerBase.m_CrouchHeight / 2f, 0f) : new Vector3(0f, this.m_ControllerBase.m_StandHeight / 2f, 0f);

            //Lerp the camera holder position to crouch or standing height
            this.m_PlayerCamera.m_CameraHolder.localPosition = Vector3.Lerp(this.m_PlayerCamera.m_CameraHolder.localPosition, new Vector3(0, _crouching ? this.m_ControllerBase.m_CameraCrouchHeight : this.m_ControllerBase.m_CameraStandHeight, 0), this.m_ControllerBase.m_CameraHeightAdjustSpeed * Time.deltaTime);
        }

        private void HandleSliding()
        {
            //Start sliding
            if (this.m_MoveState == r_MoveState.SPRINTING && this.m_InputManager.GetCrouch() && this.m_slideTime > this.m_ControllerBase.m_CanSlideLength) SetSlide(true);

            if (this.m_MoveState == r_MoveState.SLIDING)
            {
                //Set move direction x and z to slide direction
                this.m_moveDirection.x = this.m_slideDirection.x * this.m_slideTime;
                this.m_moveDirection.z = this.m_slideDirection.z * this.m_slideTime;

                //Decrease slide time while sliding
                this.m_slideTime -= Time.deltaTime * this.m_ControllerBase.m_slideTimeDecreaseMultiplier;

                //Stop slide and reset data
                if (this.m_slideTime <= this.m_ControllerBase.m_slideStopLength) SetSlide(false);

            }
            else
            {
                //When player is not sliding, increase the slide time
                if (this.m_slideTime < this.m_ControllerBase.m_slideLength) this.m_slideTime += Time.deltaTime * this.m_ControllerBase.m_slideTimeIncreaseMultiplier;
            }
        }

        private void HandleStamina()
        {
            if (this.m_MoveState == r_MoveState.SPRINTING)
            {
                //Running makes the stamina decrease
                this.m_stamina -= this.m_ControllerBase.m_staminaReduceSpeed * Time.deltaTime;

                //Stop use stamina if zero
                if (this.m_stamina <= 0)
                {
                    this.m_stamina = 0;
                    this.m_canUseStamina = false;
                }
            }
            else
            {
                //Player can start using stamina again if the current stamina is greater then stamina usable amount
                if (this.m_stamina >= this.m_ControllerBase.m_staminaUsable) this.m_canUseStamina = true;

                if (this.m_stamina < this.m_ControllerBase.m_maxStamina)
                {
                    //recover stamina
                    this.m_stamina += this.m_ControllerBase.m_staminaRecoverSpeed * Time.deltaTime;
                }
                else if (this.m_stamina >= this.m_ControllerBase.m_maxStamina) this.m_stamina = this.m_ControllerBase.m_maxStamina;
            }
        }

        private void HandleMovementStatesAudio() => this.m_PlayerAudio.m_MoveState = this.m_MoveState;
        #endregion

        #region Actions
        private void OnJump()
        {
            //Camera Jump
            this.m_PlayerCamera.onCameraFall(true);

            //Weapon bounce motion jump
            this.m_WeaponManager.OnWeaponFallMotion(true);
        }

        private void OnLand()
        {
            //Save position Y on land to calculate between start jump position y
            this.m_endFallPositionY = this.transform.position.y;

            //Check if the fall distance is greater then fall damage height, if so damage player
            if ((this.m_startFallPositionY - this.m_endFallPositionY) >= this.m_PlayerHealth.m_HealthBase.m_FallDamageHeight)
            {
                //Decrease fall damage
                this.m_PlayerHealth.DecreaseHealth(PhotonNetwork.LocalPlayer.NickName, (this.m_startFallPositionY - this.m_endFallPositionY) * this.m_PlayerHealth.m_HealthBase.m_FallDamageMultiplier, this.gameObject.transform.position, 0, null);
            }

            //Reset jumppad
            this.m_EnteredJumppad = false;

            //Reset jump count
            this.m_JumpCount = 0;

            //Reset wall jump count
            this.m_WalljumpCount = 0;

            //Camera Land
            this.m_PlayerCamera.onCameraFall(false);

            //Weapon bounce motion Land 
            this.m_WeaponManager.OnWeaponFallMotion(false);
        }
        #endregion

        #region Get
        public bool CanStandup()
        {
            float _crouchObstacleDistance;

            //Check from the bottom of the character controller to up to detect a obstacle
            if (Physics.SphereCast(this.transform.position + this.m_CharacterController.center - new Vector3(0, this.m_CharacterController.height / 2, 0), this.m_CharacterController.radius, transform.up, out RaycastHit _hit, 5f))
            {
                //Set the distance from bottom to obstacle, 
                _crouchObstacleDistance = _hit.distance;
            }
            else
            {
                //Set to infinity so the player can stand up when there is no obstacle
                _crouchObstacleDistance = Mathf.Infinity;
            }

            //The player can stand up if the distance between character controller bottom to obstacle is greater then standheight
            return _crouchObstacleDistance >= this.m_ControllerBase.m_StandHeight;
        }

        public bool OnMaxSlope()
        {
            //Check raycast under player position
            if (Physics.Raycast(this.transform.position, Vector3.down, out m_raycastHit, (this.m_CharacterController.height / 2) + 0.1f))
            {
                //The player is on slope if the standing on object angle is more then the slope limit
                return Vector3.Angle(Vector3.up, m_raycastHit.normal) > this.m_ControllerBase.m_slopeLimit;
            }
            return false;
        }

        public Vector3 GetSlopeSlideDirection() => new Vector3(this.m_raycastHit.normal.x, -this.m_raycastHit.normal.y, this.m_raycastHit.normal.z).normalized;

        public Vector3 GetNormalizedDirection() => Vector3.ProjectOnPlane(this.transform.forward, this.m_raycastHit.normal).normalized;
        #endregion

        #region Set
        private void SetDefaults()
        {
            //Enable inputs
            this.m_InputManager.m_Controllable = true;

            //Deactive ragdoll
            this.m_ThirdPersonManager.DeActivateRagdoll();

            //Set full stamina
            this.m_stamina = this.m_ControllerBase.m_maxStamina;

            //Set full slide time
            this.m_slideTime = this.m_ControllerBase.m_slideLength;
        }

        private void SetJump(float _height)
        {
            //Add jump count
            this.m_JumpCount++;

            //Jump sound
            this.m_PlayerAudio.OnPlayerJumpAudioPlay(this.transform.position);

            //Jump
            this.m_moveDirection.y = _height;

            //On Jump
            OnJump();
        }

        private void SetSlide(bool _state)
        {
            //Play slide sound
            if (this.m_PhotonView.IsMine) this.m_PlayerAudio.UpdateSlideSound(_state);

            //Find slide speed
            float _slide_speed = this.m_ControllerBase.m_MoveStateSettings.Find(x => x.m_MoveState == r_MoveState.SLIDING).m_MoveSpeed;

            //Set slide direction based on input
            this.m_slideDirection = _state == true ? this.m_InputMovement * _slide_speed : Vector3.zero;

            //Set move state
            this.m_MoveState = _state == true ? r_MoveState.SLIDING : r_MoveState.CROUCHING;
        }

        private void DecreaseStamina(float _amount) => this.m_stamina -= _amount;
        private void IncreaseStamina(float _amount) => this.m_stamina += _amount;
        private void SetMoveState(r_MoveState _state) => this.m_MoveState = _state;
        #endregion

        #region Custom
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            //Wall jump
            if (this.m_WalljumpCount < this.m_ControllerBase.m_MaxWallJumpCount && this.m_MoveState == r_MoveState.JUMPING && hit.normal.y < 0.5f)
            {
                if (this.m_InputManager.GetJump())
                {
                    //Jump sound
                    this.m_PlayerAudio.OnPlayerJumpAudioPlay(this.transform.position);

                    //Check jump direction
                    Vector3 _jump_direction = new Vector3(hit.normal.x, 0.0f, hit.normal.z) * this.m_CurrentSpeed;

                    //Add movedirection so the current position doesn't reset and the player can keep moving
                    this.m_moveDirection += _jump_direction;

                    //Add jump height
                    this.m_moveDirection.y = this.m_ControllerBase.m_JumpHeight;

                    //Count walljump count
                    this.m_WalljumpCount++;
                }
            }

            //Handle jumppad
            r_WorldJumppad _jump_pad = hit.gameObject.GetComponent<r_WorldJumppad>();

            if (_jump_pad != null)
            {
                //Set jumppad force
                this.m_JumppadForce = _jump_pad.m_JumpForce;

                //Set 
                this.m_EnteredJumppad = true;
            }

            //Handle footsteps
            if (this.m_PhotonView.IsMine)
            {
                if (this.m_grounded && this.m_CharacterController.velocity.magnitude > 0.5f && hit.normal.y > 0.8)
                {
                    //Play footstep audio
                    this.m_PlayerAudio.HandleFootsteps(hit.collider);
                }
            }
        }
        #endregion
    }
}