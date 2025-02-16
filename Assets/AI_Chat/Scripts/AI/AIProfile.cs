using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IDosGames
{
    public class AIProfile : MonoBehaviour
    {
        [SerializeField] private TMP_Text _aiName;
        [SerializeField] private Image _aiIcon;

        private void Start()
        {
            UpdateAvatar();
        }


        private void OnEnable()
        {
            ImageLoader.ImagesUpdated += UpdateAvatar;
        }

        private void OnDisable()
        {
            ImageLoader.ImagesUpdated -= UpdateAvatar;
        }

        private void UpdateAvatar()
        {
            _aiName.text = ChatView._aiName;

            if (ChatView._aiAvatarSprite != null)
            {
                _aiIcon.sprite = ChatView._aiAvatarSprite;
            }
        }

        public void ShowBuyCoinPopup()
        {
            ShopSystem.PopUpSystem.ShowShopWindow();
        }

        public void ShareReferralLink()
        {
            ReferralSystem.Share();
        }
    }
}
