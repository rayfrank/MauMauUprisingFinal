using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

namespace ForceCodeFPS
{
    public class r_PlayerAudio : MonoBehaviourPun
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

        #region Public variables
        [Header("Audio Base Configuration")]
        public r_PlayerAudioBase m_AudioConfig;

        [Header("AudioSource")]
        public AudioSource m_AudioSource;
        #endregion

        #region Private variables
        //Current player movement state
        [HideInInspector] public r_MoveState m_MoveState;

        //Current footstep Last step
        [HideInInspector] public float m_LastStep;

        //Last footstep clip
        [HideInInspector] public AudioClip m_LastFootstepClip;
        #endregion

        #region Actions
        public void OnWeaponAudioPlay(string _weapon_name, string _audio_clip_name, float _audio_volume, bool _networked)
        {
            if (_networked)
            {
                //Play audio over network
                photonView.RPC(nameof(OnWeaponAudioPlay_RPC), RpcTarget.All, _weapon_name, _audio_clip_name, _audio_volume);
            }
            else
            {
                //Find audio local
                AudioClip _audio_clip = FindWeaponAudioClip(_weapon_name, _audio_clip_name, _audio_volume);

                if (_audio_clip != null)
                {
                    //Play clip at point locally
                    AudioSource.PlayClipAtPoint(_audio_clip, this.m_AudioSource.transform.position, _audio_volume);
                }
            }
        }

        private void UpdateFootstepSound(Vector3 _position, Collider _collider, float _footstepLength)
        {
            r_Surface _surface = FindSurface(_position, _collider);

            if (_surface != null)
            {
                //Find Surface index
                int _surface_index = this.m_AudioConfig.m_Surfaces.FindIndex(x => x == _surface);

                if (this.m_AudioConfig.m_Surfaces[_surface_index] != null)
                {
                    //Specific footstep play
                    photonView.RPC(nameof(PlayFootstepSound), RpcTarget.All, _position, _surface_index, this.m_MoveState.ToString());
                }
            }
            else
            {
                r_Surface _default_surface = FindDefaultSurface();

                if (_default_surface != null)
                {
                    //Find Surface index
                    int _default_surface_index = this.m_AudioConfig.m_Surfaces.FindIndex(x => x == _default_surface);

                    if (this.m_AudioConfig.m_Surfaces[_default_surface_index] != null)
                    {
                        //Default footstep play
                        photonView.RPC(nameof(PlayFootstepSound), RpcTarget.All, _position, _default_surface_index, this.m_MoveState.ToString());
                    }
                }
            }
        }

        public void UpdateSlideSound(bool _state) => photonView.RPC(nameof(PlaySlideSound), RpcTarget.All, this.transform.position, _state);

        public void OnPlayerHurtAudioPlay(Vector3 _position) => photonView.RPC(nameof(OnPlayerHurtAudioPlay_RPC), RpcTarget.All, _position);
        public void OnPlayerJumpAudioPlay(Vector3 _position) => photonView.RPC(nameof(OnPlayerJumpAudioPlay_RPC), RpcTarget.All, _position);
        #endregion

        #region Handling
        public void HandleFootsteps(Collider _collider)
        {
            if (this.m_MoveState == r_MoveState.SLIDING) return;

            if (Time.time >= m_LastStep)
            {
                //Reset last step 
                this.m_LastStep = 0;

                //Add footstep length for delay
                this.m_LastStep = Time.time + GetCurrentFootstepState(this.m_MoveState).m_FootstepLength;

                //Check footstep sounds
                UpdateFootstepSound(this.transform.position, _collider, GetCurrentFootstepState(this.m_MoveState).m_FootstepLength);
            }
        }
        #endregion

        #region Network Events
        [PunRPC]
        private void PlayFootstepSound(Vector3 _position, int _index, string _moveState)
        {
            if (this.m_AudioSource == null) return;

            //Find surface by ID 
            r_Surface _surface = this.m_AudioConfig.m_Surfaces[_index];

            //Clip to play
            AudioClip _clip = GetRandomFootstepClip(_surface.GetFootstepClips());

            //Volume
            this.m_AudioSource.volume = GetCurrentFootstepStateByName(_moveState).m_FootstepVolume;

            //Play audio
            AudioSource.PlayClipAtPoint(_clip, this.m_AudioSource.transform.position, this.m_AudioSource.volume);

            //Save last audioclip
            this.m_LastFootstepClip = _clip;
        }

        [PunRPC]
        private void PlayBulletImpactSound(Vector3 _position, int _index)
        {
            if (this.m_AudioSource == null) return;

            //Find surface by ID 
            r_Surface _surface = this.m_AudioConfig.m_Surfaces[_index];

            //Play audio
            AudioSource.PlayClipAtPoint(_surface.GetBulletImpactClip(), _position, this.m_AudioConfig.m_BulletImpactAudioVolume);
        }

        [PunRPC]
        private void PlaySlideSound(Vector3 _position, bool _state)
        {
            this.m_AudioSource.loop = _state;
            this.m_AudioSource.volume = _state ? 1 : 0;

            this.m_AudioSource.clip = this.m_AudioConfig.m_SlideClip;

            AudioSource.PlayClipAtPoint(this.m_AudioSource.clip, this.m_AudioSource.transform.position, this.m_AudioSource.volume);
        }

        [PunRPC]
        private void OnPlayerHurtAudioPlay_RPC(Vector3 _position)
        {
            if (this.m_AudioSource.isPlaying) return;

            this.m_AudioSource.PlayOneShot(this.m_AudioConfig.m_HurtSound, this.m_AudioConfig.m_HurtAudioVolume);
        }

        [PunRPC]
        private void OnPlayerJumpAudioPlay_RPC(Vector3 _position) => AudioSource.PlayClipAtPoint(this.m_AudioConfig.m_JumpSound, _position, this.m_AudioConfig.m_JumpAudioVolume);

        [PunRPC]
        private void OnWeaponAudioPlay_RPC(string _weapon_name, string _audio_clip_name, float _audio_volume)
        {
            AudioClip _audio_clip = FindWeaponAudioClip(_weapon_name, _audio_clip_name, _audio_volume);

            if (_audio_clip != null)
            {
                //Play audio at position
                AudioSource.PlayClipAtPoint(_audio_clip, this.m_AudioSource.transform.position, _audio_volume);
            }
        }
        #endregion

        #region Get
        public r_Surface FindDefaultSurface() => this.m_AudioConfig.m_Surfaces.Find(x => x.m_SurfaceType == r_SurfaceType.DEFAULT && x.m_Textures.Count == 0);

        public r_Surface FindSurface(Vector3 _position, Collider _collider)
        {
            r_Surface _surface = null;

            this.m_AudioConfig.m_Surfaces.ForEach(x =>
            {
                if (x.m_Textures.Count > 0)
                {
                //Find surface
                _surface = this.m_AudioConfig.m_Surfaces.Find(I => I.m_Textures.Contains(CheckTexture(_position, _collider)));
                }
            });

            return _surface;
        }

        public AudioClip FindWeaponAudioClip(string _weapon_name, string _audio_clip_name, float _audio_volume)
        {
            r_WeaponController _weapon = this.m_WeaponManager.FindWeaponByName(_weapon_name).m_WeaponData.m_Weapon_FP_Prefab.GetComponent<r_WeaponController>();

            if (_weapon != null)
            {
                AudioClip _audio_clip = _weapon.m_WeaponConfig.m_WeaponSound.m_AudioClips.Find(x => x.name == _audio_clip_name);

                if (_audio_clip != null)
                    return _audio_clip;
            }
            return null;
        }

        public GameObject GetBulletImpact(Collider _collider, Vector3 _hitPoint)
        {
            r_Surface _surface = FindSurface(_hitPoint, _collider);

            if (_surface != null)
            {
                //Find Surface index
                int _surface_index = this.m_AudioConfig.m_Surfaces.FindIndex(x => x == _surface);

                if (this.m_AudioConfig.m_Surfaces[_surface_index] != null)
                {
                    //Specific footstep play
                    photonView.RPC(nameof(PlayBulletImpactSound), RpcTarget.All, _hitPoint, _surface_index);

                    //Return surface bullet impact to instantiate in r_WeaponController
                    return this.m_AudioConfig.m_Surfaces[_surface_index].m_BulletImpact;
                }
            }
            else
            {
                r_Surface _default_surface = FindDefaultSurface();

                if (_default_surface != null)
                {
                    //Find Surface index
                    int _default_surface_index = this.m_AudioConfig.m_Surfaces.FindIndex(x => x == _default_surface);

                    if (this.m_AudioConfig.m_Surfaces[_default_surface_index] != null)
                    {
                        //Default footstep play
                        photonView.RPC(nameof(PlayBulletImpactSound), RpcTarget.All, _hitPoint, _default_surface_index);

                        //Return surface bullet impact to instantiate in r_WeaponController
                        return this.m_AudioConfig.m_Surfaces[_default_surface_index].m_BulletImpact;
                    }
                }
            }

            return null;
        }

        private Texture2D CheckTexture(Vector3 _position, Collider _collider)
        {
            Texture2D _texture = null;

            if (_collider.GetComponent<Terrain>())
            {
                //on terrain
                _texture = Terrain.activeTerrain.terrainData.terrainLayers[GetTerrainTextureID(_position)].diffuseTexture;
            }
            else if (_collider.GetComponent<Renderer>())
            {
                //on gameobject
                _texture = GetRendererTexture(_collider);
            }
            return _texture;
        }

        private Texture2D GetRendererTexture(Collider _collider) => (Texture2D)_collider.GetComponent<Renderer>().material.mainTexture;

        private Vector3 GetTerrainCoordinate(Vector3 _position)
        {
            //Declare current terrain
            Terrain _currentTerrain = Terrain.activeTerrain;

            //Check the terrain sizes
            int _mapX = (int)(((_position.x - _currentTerrain.transform.position.x) / _currentTerrain.terrainData.size.x) * _currentTerrain.terrainData.alphamapWidth);
            int _mapZ = (int)(((_position.z - _currentTerrain.transform.position.z) / _currentTerrain.terrainData.size.z) * _currentTerrain.terrainData.alphamapHeight);

            //Return map coordinates
            return new Vector3(_mapX, 0, _mapZ);
        }

        private int GetTerrainTextureID(Vector3 _position)
        {
            //Get terrain coordinates
            Vector3 _terrainCoordinate = GetTerrainCoordinate(_position);

            //Get alpha maps
            float[,,] _alphaMaps = Terrain.activeTerrain.terrainData.GetAlphamaps((int)_terrainCoordinate.x, (int)_terrainCoordinate.z, 1, 1);
            float[] _cellMix = new float[_alphaMaps.GetUpperBound(2) + 1];

            float _maxMix = 0;

            for (int i = 0; i < _cellMix.Length; i++) _cellMix[i] = _alphaMaps[0, 0, i];

            int _maxIndex = 0;

            for (int i = 0; i < _cellMix.Length; ++i)
            {
                if (_cellMix[i] > _maxMix)
                {
                    _maxIndex = i;
                    _maxMix = _cellMix[i];
                }
            }
            return _maxIndex;
        }

        public AudioClip GetRandomFootstepClip(AudioClip[] _audioclips)
        {
            int _attempts = 3;

            AudioClip _selectedClip = _audioclips[Random.Range(0, _audioclips.Length)];

            while (_selectedClip == this.m_LastFootstepClip && _attempts > 0)
            {
                _selectedClip = _audioclips[Random.Range(0, _audioclips.Length)];
                _attempts--;
            }

            return _selectedClip;
        }

        public r_SurfaceMoveSetting GetCurrentFootstepState(r_MoveState _moveState) => this.m_AudioConfig.m_FootstepMoveSettings.Find(x => x.m_MoveState == _moveState);
        public r_SurfaceMoveSetting GetCurrentFootstepStateByName(string _moveState) => this.m_AudioConfig.m_FootstepMoveSettings.Find(x => x.m_MoveState.ToString() == _moveState);
        #endregion
    }
}