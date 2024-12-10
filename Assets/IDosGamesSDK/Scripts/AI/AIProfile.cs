using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IDosGames
{
    public class AIProfile : MonoBehaviour
    {
        [SerializeField] private TMP_Text _aiName;
        [SerializeField] private Image _aiIcon;

        private async void Start()
        {
            string aiName = UserDataService.TitlePublicConfiguration.AiSettings.AiName;

            if (string.IsNullOrEmpty(aiName))
            {
                _aiName.text = "AI";
            }
            else
            {
                _aiName.text = aiName;
            }

            string aiAvatarUrl = UserDataService.TitlePublicConfiguration.AiSettings.AiAvatarUrl;

            if (!string.IsNullOrEmpty(aiAvatarUrl))
            {
                Sprite aiAvatarSprite = await LoadAiAvatarAsync(aiAvatarUrl);

                if (aiAvatarSprite != null)
                {
                    _aiIcon.sprite = aiAvatarSprite;
                }
                else
                {
                    Debug.LogError("Failed to load AI avatar.");
                }
            }
        }

        private async Task<Sprite> LoadAiAvatarAsync(string url)
        {
            return await ImageLoader.GetSpriteAsync(url);
        }

        public void ShowBuyCoinPopup()
        {
            ShopSystem.PopUpSystem.ShowCoinPopUp();
        }
    }
}
