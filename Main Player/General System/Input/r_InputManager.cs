using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceCodeFPS
{
    public struct r_InputManager
    {
        #region Inputs
        public bool m_Controllable;

        /* Movement */
        public float GetVertical() => this.m_Controllable ? Input.GetAxis("Vertical") : 0;
        public float GetHorizontal() => this.m_Controllable ? Input.GetAxis("Horizontal") : 0;

        public bool GetJump() => this.m_Controllable ? Input.GetKeyDown(KeyCode.Space) : false;

        public bool GetCrouch() => this.m_Controllable ? Input.GetKeyDown(KeyCode.C) : false;
        public bool GetSprint() => this.m_Controllable ? Input.GetKey(KeyCode.LeftShift) : false;

        /* Mouse */
        public float GetMouseX() => this.m_Controllable ? Input.GetAxisRaw("Mouse X") : 0;
        public float GetMouseY() => this.m_Controllable ? Input.GetAxisRaw("Mouse Y") : 0;

        /* Camera */
        public bool GetLeanLeftKey() => this.m_Controllable ? Input.GetKey(KeyCode.Q) : false;
        public bool GetLeanRightKey() => this.m_Controllable ? Input.GetKey(KeyCode.E) : false;

        /* Weapon */
        public bool GetFireClick() => this.m_Controllable ? Input.GetKeyDown(KeyCode.Mouse0) : false;
        public bool GetFireHold() => this.m_Controllable ? Input.GetKey(KeyCode.Mouse0) : false;
        public bool GetReloadKey() => this.m_Controllable ? Input.GetKeyDown(KeyCode.R) : false;
        public bool GetAimKey() => this.m_Controllable ? Input.GetKey(KeyCode.Mouse1) : false;

        /* Weapon Manager */
        public bool WeaponPickKey() => this.m_Controllable ? Input.GetKeyDown(KeyCode.F) : false;
        public bool WeaponDropKey() => this.m_Controllable ? Input.GetKeyDown(KeyCode.G) : false;
        #endregion
    }
}