using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace IDosGames
{
    public class ChatView : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _sendButton;

        [Header("Prefabs")]
        [Space(10)]
        [SerializeField] private ChatMessage _botMessagePrefab;
        [SerializeField] private ChatMessage _userMessagePrefab;
        [SerializeField] private ChatMessage _inviteMessagePrefab;
        [SerializeField] private ChatMessage _buyMessagePrefab;

        [Header("Loading")]
        [Space(10)]
        [SerializeField] private GameObject _loading;

        private const float TYPE_SPEED = 50f;
        private const int MIN_MESSAGE_LENGTH = 2;
        private const int MAX_MESSAGE_LENGTH = 1000;
        private List<MessageAI> messages = new List<MessageAI>();
        private static string MESSAGE_HISTORY_KEY = "MessageHistory" + AuthService.UserID;
        private string _welcomeMessage;

        private bool _isUserScrolling = false;
        private float _scrollInactivityTime = 0f;
        private const float INACTIVITY_THRESHOLD = 6f;

        public static string _aiName;
        public static Sprite _aiAvatarSprite;

        private void Start()
        {
            _isUserScrolling = false;
            _scrollInactivityTime = INACTIVITY_THRESHOLD;

            SetActivateLoading(false);
            SetInteractableSendButton(true);
            _sendButton.onClick.AddListener(SendUserMessage);
            _inputField.onValueChanged.AddListener(CheckInputLength);
            _inputField.onSelect.AddListener(OnInputFieldSelected);
            _inputField.onDeselect.AddListener(OnInputFieldDeselected);
            ClearInput();

            _scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

            StartScrollToBottom();
        }

        private void OnDestroy()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);
            _inputField.onSelect.RemoveListener(OnInputFieldSelected);
            _inputField.onDeselect.RemoveListener(OnInputFieldDeselected);
        }

        private void OnEnable()
        {
            ImageLoader.ImagesUpdated += UpdateAvatar;
            UserDataService.FirstTimeDataUpdated += FirstMessage;
            SetActivateLoading(false);
            SetInteractableSendButton(true);
        }

        private void OnDisable()
        {
            ImageLoader.ImagesUpdated -= UpdateAvatar;
            UserDataService.FirstTimeDataUpdated -= FirstMessage;
            StopAllCoroutines();
        }

        private void Update()
        {
            if (_isUserScrolling)
            {
                _scrollInactivityTime += Time.deltaTime;
                if (_scrollInactivityTime > INACTIVITY_THRESHOLD)
                {
                    _isUserScrolling = false;
                }
            }
        }

        private async void UpdateAvatar()
        {
            _aiName = string.IsNullOrEmpty(UserDataService.TitlePublicConfiguration.AiSettings.AiName) ? "AI" : UserDataService.TitlePublicConfiguration.AiSettings.AiName;

            string aiAvatarUrl = UserDataService.TitlePublicConfiguration.AiSettings.AiAvatarUrl;

            if (!string.IsNullOrEmpty(aiAvatarUrl))
            {
                Sprite aiAvatarSprite = await ImageLoader.GetSpriteAsync(aiAvatarUrl);

                if (aiAvatarSprite != null)
                {
                    _aiAvatarSprite = aiAvatarSprite;
                }
                else
                {
                    Debug.LogError("Failed to load AI avatar.");
                }
            }
        }

        private void OnScrollRectValueChanged(Vector2 position)
        {
            if (IsUserInteractingWithScroll())
            {
                _isUserScrolling = true;
                _scrollInactivityTime = 0f;
            }
        }

        private void OnInputFieldSelected(string text)
        {
            // ��������� � ���������� ���������, ����� ��� ��������� � �������� ������
        }

        private void OnInputFieldDeselected(string text)
        {
            // ����� �������� ������, ���� ��������� ���-�� ������� ��� ������ ������  
        }

        private bool IsUserInteractingWithScroll()
        {
            return Input.GetMouseButton(0) || Input.touchCount > 0;
        }

        private void FirstMessage()
        {
            bool isHistoryLoaded = LoadMessageHistory();
            if (!isHistoryLoaded)
            {
                if (string.IsNullOrEmpty(UserDataService.TitlePublicConfiguration.AiSettings.AiWelcomeMessage))
                {
                    _welcomeMessage = "Hi! Can I help you?";
                }
                else
                {
                    _welcomeMessage = UserDataService.TitlePublicConfiguration.AiSettings.AiWelcomeMessage;
                }
                SendBotMessage(_welcomeMessage);
            }
        }

        private void CheckInputLength(string input)
        {
            if (input.Contains("\n"))
            {
                SendUserMessage();
                ClearInput();
            }
            else
            {
                SetInteractableSendButton(input.Length >= MIN_MESSAGE_LENGTH && input.Length <= MAX_MESSAGE_LENGTH);
            }
        }

        public async void SendUserMessage()
        {
            _isUserScrolling = false;
            _scrollInactivityTime = INACTIVITY_THRESHOLD;
            string message = GetInputMessage().Replace("\n", "");
            if (message.Length < MIN_MESSAGE_LENGTH || message.Length > MAX_MESSAGE_LENGTH)
            {
                Debug.LogWarning("Message length is out of range.");
                return;
            }

            string currencyCode = UserDataService.TitlePublicConfiguration.AiSettings.AiRequestCurrency;
            int amountToDeduct = UserDataService.TitlePublicConfiguration.AiSettings.AiRequestCurrencyAmount;
            int currentAmount = IGSUserData.UserInventory.VirtualCurrency.GetValueOrDefault(currencyCode, 0);
            if (currentAmount < amountToDeduct)
            {
                string buyMessage = "It looks like you have run out of coins. Coins are needed to send messages and can be obtained by inviting friends, viewing ads or buying them.";
                SendBuyMessage(buyMessage);
                return;
            }

            SendMessagePrefab(_userMessagePrefab, message);
            ClearInput();
            StartScrollToBottom();
            SetActivateLoading(true);

            messages.Add(new MessageAI
            {
                Role = "user",
                Content = message
            });
            SaveMessages(messages);

            string aiResponse = await GetAIResponse();
            if (aiResponse != null)
            {
                if (aiResponse.Contains("INSUFFICIENT_FUNDS"))
                {
                    string buyMessage = "It looks like you have run out of coins. Coins are needed to send messages and can be obtained by inviting friends, viewing ads or buying them.";
                    SendBuyMessage(buyMessage);
                }
                else
                {
                    SendBotMessage(aiResponse);
                }
            }
        }

        private async Task<string> GetAIResponse()
        {
            var request = new AIRequest
            {
                Messages = messages
            };
            string response = await AIService.CreateThreadAndRun(request);
            if (response != null)
            {
                if (!response.Contains("INSUFFICIENT_FUNDS"))
                {
                    string currencyCode = UserDataService.TitlePublicConfiguration.AiSettings.AiRequestCurrency;
                    int amountToDeduct = UserDataService.TitlePublicConfiguration.AiSettings.AiRequestCurrencyAmount;
                    int currentAmount = IGSUserData.UserInventory.VirtualCurrency.GetValueOrDefault(currencyCode, 0);
                    int newAmount = currentAmount - amountToDeduct;
                    IGSUserData.UserInventory.VirtualCurrency[currencyCode] = newAmount;
                    UserDataService.VirtualCurrencyUpdatedInvoke();
                }
            }
            return response;
        }

        public void SendBotMessage(string message)
        {
            _isUserScrolling = false;
            _scrollInactivityTime = INACTIVITY_THRESHOLD;
            StartCoroutine(TypeTextWithCoroutine(message, Instantiate(_botMessagePrefab, _scrollRect.content.transform)));
            SetActivateLoading(false);
            messages.Add(new MessageAI
            {
                Role = "assistant",
                Content = message
            });
            SaveMessages(messages);
        }

        public void SendBuyMessage(string message)
        {
            _isUserScrolling = false;
            _scrollInactivityTime = INACTIVITY_THRESHOLD;
            StartCoroutine(TypeTextWithCoroutine(message, Instantiate(_buyMessagePrefab, _scrollRect.content.transform)));
            SetActivateLoading(false);
            messages.Add(new MessageAI
            {
                Role = "assistant",
                Content = message
            });
        }

        private IEnumerator TypeTextWithCoroutine(string message, ChatMessage botMessage)
        {
            string text = string.Empty;
            foreach (char c in message)
            {
                text += c;
                botMessage.Set(text);
                StartScrollToBottom();
                yield return new WaitForSeconds(1f / TYPE_SPEED);
            }
        }

        private void SetInteractableSendButton(bool interactable)
        {
            _sendButton.interactable = interactable;
        }

        public string GetInputMessage()
        {
            return _inputField.text;
        }

        private void ClearInput()
        {
            _inputField.text = string.Empty;
            CheckInputLength(string.Empty);
        }

        private void StartScrollToBottom()
        {
            if (!_isUserScrolling)
            {
                StartCoroutine(nameof(ScrollToTop));
            }
        }

        private IEnumerator ScrollToTop()
        {
            yield return new WaitForEndOfFrame();
            if (!_isUserScrolling) // ��� ��� ��������� ����� ����������  
            {
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void SetActivateLoading(bool active)
        {
            if (active)
            {
                _loading.transform.SetParent(_scrollRect.content.transform);
            }
            else
            {
                _loading.transform.SetParent(_scrollRect.transform);
            }
            _loading.SetActive(active);
        }

        private void SendMessagePrefab(ChatMessage prefab, string message)
        {
            var userMessage = Instantiate(prefab, _scrollRect.content.transform);
            userMessage.Set(message);
        }

        private bool LoadMessageHistory()
        {
            string json = PlayerPrefs.GetString(MESSAGE_HISTORY_KEY, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                List<MessageAI> loadedMessages = JsonConvert.DeserializeObject<List<MessageAI>>(json);
                foreach (var message in loadedMessages)
                {
                    if (message.Role == "user")
                    {
                        var userMessage = Instantiate(_userMessagePrefab, _scrollRect.content.transform);
                        userMessage.Set(message.Content);
                    }
                    else if (message.Role == "assistant")
                    {
                        var botMessage = Instantiate(_botMessagePrefab, _scrollRect.content.transform);
                        botMessage.Set(message.Content);
                    }
                    messages.Add(message);
                }
                return true;
            }
            return false;
        }

        private void SaveMessages(List<MessageAI> messages)
        {
            string json = JsonConvert.SerializeObject(messages);
            PlayerPrefs.SetString(MESSAGE_HISTORY_KEY, json);
            PlayerPrefs.Save();
        }

        public void ClearMessageHistory()
        {
            PlayerPrefs.DeleteKey(MESSAGE_HISTORY_KEY);
            PlayerPrefs.Save();
            messages.Clear();
            foreach (Transform child in _scrollRect.content.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
