using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    public class r_GraphicOptions : MonoBehaviour
    {
        #region Public Variables
        [Header("Graphic UI")]
        public Slider m_VolumeSlider;
        public Dropdown m_ResolutionDropdown;
        public Dropdown m_QualityDropdown;

        [Space(10)]

        public Toggle m_FullScreenToggle;
        public Toggle m_VSyncToggle;

        [Space(10)]

        [Space(10)] public Button m_SaveOptionsButton;

        [Space(10)] public List<string> m_QualityOptions = new List<string>();
        #endregion

        #region Private Variables
        //Resolutions
        private Resolution[] m_ResolutionList;
        #endregion

        #region Functions
        private void Awake() => HandleButtons();

        private void Start() => Setup();
        #endregion

        #region Actions
        private void HandleButtons() => this.m_SaveOptionsButton.onClick.AddListener(delegate { SaveOptions(); });

        private void Setup()
        {
            LoadResolutionOptions();
            LoadQualityOptions();
            LoadFullscreen();
            LoadVSyncOption();
            LoadVolumeOption();
        }
        #endregion

        #region Set
        private void SetVolume(float _volume)
        {
            //Save volume pref audio listener volume
            PlayerPrefs.SetFloat("Volume", _volume);

            //Set audio listener volume
            AudioListener.volume = PlayerPrefs.GetFloat("Volume");
        }

        private void SetFullscreen(bool _state) => Screen.fullScreen = _state;
        private void SetVSync(bool _state) => QualitySettings.vSyncCount = _state ? 1 : 0;
        private void SetQuality(int _index) => QualitySettings.SetQualityLevel(_index);
        private void SetResolution(int _index) => Screen.SetResolution(this.m_ResolutionList[_index].width, this.m_ResolutionList[_index].height, Screen.fullScreen);

        private void SaveOptions()
        {
            SetResolution(this.m_ResolutionDropdown.value);
            SetFullscreen(this.m_FullScreenToggle.isOn ? true : false);
            SetQuality(this.m_QualityDropdown.value);
            SetVSync(this.m_VSyncToggle.isOn ? true : false);
            SetVolume(this.m_VolumeSlider.value);
        }
        #endregion

        #region Get
        private void LoadResolutionOptions()
        {
            //Load
            this.m_ResolutionList = Screen.resolutions;

            //Clear
            this.m_ResolutionDropdown.ClearOptions();

            //Declare
            int _resolution_index = 0;

            //Set resolution options for dropdown
            List<Dropdown.OptionData> _resolution_options = new List<Dropdown.OptionData>();

            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                _resolution_options.Add(new Dropdown.OptionData { text = Screen.resolutions[i].width + "x" + Screen.resolutions[i].height + "@" + Screen.resolutions[i].refreshRate + "Hz" });

                if (this.m_ResolutionList[i].width == Screen.currentResolution.width && this.m_ResolutionList[i].height == Screen.currentResolution.height && this.m_ResolutionList[i].refreshRate == Screen.currentResolution.refreshRate)
                {
                    _resolution_index = i;
                }
            }

            //Add resolutions to dropdown
            this.m_ResolutionDropdown.AddOptions(_resolution_options);

            //Select current resolution in dropdown
            this.m_ResolutionDropdown.value = _resolution_index;

            //Refresh
            this.m_ResolutionDropdown.RefreshShownValue();
        }

        private void LoadQualityOptions()
        {
            //Add options
            this.m_QualityDropdown.AddOptions(this.m_QualityOptions);

            //Select current value
            this.m_QualityDropdown.value = QualitySettings.GetQualityLevel();

            //Refresh
            this.m_QualityDropdown.RefreshShownValue();
        }

        private void LoadFullscreen()
        {
            //Load current fullscreen on toggle
            this.m_FullScreenToggle.isOn = Screen.fullScreen == true ? true : false;
        }

        private void LoadVSyncOption()
        {
            //Load current VSync value
            this.m_VSyncToggle.isOn = QualitySettings.vSyncCount == 1 ? true : false;
        }

        private void LoadVolumeOption()
        {
            if (PlayerPrefs.HasKey("Volume"))
            {
                //Get slider value
                this.m_VolumeSlider.value = PlayerPrefs.GetFloat("Volume");

                //Set audio listener volume
                AudioListener.volume = this.m_VolumeSlider.value;
            }
            else
            {
                //Full audio volume on default
                this.m_VolumeSlider.value = this.m_VolumeSlider.maxValue;

                //Set audio listener volume
                AudioListener.volume = this.m_VolumeSlider.value;

                //Save volume pref
                PlayerPrefs.SetFloat("Volume", this.m_VolumeSlider.value);
            }
        }
        #endregion
    }
}