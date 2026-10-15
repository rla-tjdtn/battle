using UnityEngine;
using UnityEngine.UI;
using BlockyBattleRoyale.Core;

namespace BlockyBattleRoyale.UI
{
    public class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sensSlider;
        [SerializeField] private Text infoText;

        private void Start()
        {
            GameSettings.ApplyAll();
            if (panel != null)
                panel.SetActive(false);

            if (masterSlider != null)
            {
                masterSlider.value = GameSettings.MasterVolume;
                masterSlider.onValueChanged.AddListener(v => { GameSettings.MasterVolume = v; GameSettings.Save(); });
            }

            if (sensSlider != null)
            {
                sensSlider.value = GameSettings.MouseSensitivity;
                sensSlider.onValueChanged.AddListener(v => { GameSettings.MouseSensitivity = v; GameSettings.Save(); });
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && panel != null)
            {
                bool open = !panel.activeSelf;
                panel.SetActive(open);
                if (infoText != null)
                    infoText.text = open ? "SETTINGS\nMaster / Sensitivity\nEsc to close" : "";
            }
        }

        public void Bind(GameObject settingsPanel, Slider master, Slider sens, Text info)
        {
            panel = settingsPanel;
            masterSlider = master;
            sensSlider = sens;
            infoText = info;
        }
    }
}
