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

        [Header("Loading")]
        [Space(10)]
        [SerializeField] private GameObject _loading;

        private const float TYPE_SPEED = 50f;
        private const int MIN_MESSAGE_LENGTH = 2;
        private const int MAX_MESSAGE_LENGTH = 500;
        private List<MessageAI> messages = new List<MessageAI>();

        private const string MESSAGE_HISTORY_KEY = "MessageHistory1";
        string _welcomeMessage;

        private void Start()
        {
            SetActivateLoading(false);
            SetInteractableSendButton(true);

            // Программно присваиваем метод кнопке
            _sendButton.onClick.AddListener(SendUserMessage);

            // Подписка на изменение текста в поле ввода
            _inputField.onValueChanged.AddListener(CheckInputLength);
            ClearInput();
        }

        private void OnEnable()
        {
            UserDataService.FirstTimeDataUpdated += FirstMessage;
            SetActivateLoading(false);
            SetInteractableSendButton(true);
        }

        private void OnDisable()
        {
            UserDataService.FirstTimeDataUpdated -= FirstMessage;
            StopAllCoroutines();
        }

        private void FirstMessage()
        {
            // Загрузка истории сообщений
            bool isHistoryLoaded = LoadMessageHistory();

            if (!isHistoryLoaded)
            {
                if (string.IsNullOrEmpty(UserDataService.TitlePublicConfiguration.AiSettings.AiWelcomeMessage))
                {
                    _welcomeMessage = "Hello, I'm a support bot, can I help you?";
                }
                else
                {
                    _welcomeMessage = UserDataService.TitlePublicConfiguration.AiSettings.AiWelcomeMessage;
                }

                SendBotMessage(_welcomeMessage);
            }
        }

        // Проверка длины ввода и установка активности кнопки
        private void CheckInputLength(string input)
        {
            SetInteractableSendButton(input.Length >= MIN_MESSAGE_LENGTH && input.Length <= MAX_MESSAGE_LENGTH);
        }

        // Отправка сообщения пользователя
        public async void SendUserMessage()
        {
            string message = GetInputMessage();
            if (message.Length < MIN_MESSAGE_LENGTH || message.Length > MAX_MESSAGE_LENGTH)
            {
                Debug.LogWarning("Message length is out of range.");
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

            // Сохранение истории сообщений
            SaveMessages(messages);

            string aiResponse = await GetAIResponse();
            SendBotMessage(aiResponse);
        }

        // Получение ответа AI
        private async Task<string> GetAIResponse()
        {
            var request = new AIRequest
            {
                Messages = messages
            };

            string response = await AIService.CreateThreadAndRun(request);

            if (response != null)
            {
                string currencyCode = UserDataService.TitlePublicConfiguration.AiSettings.AiRequestCurrency;
                int amountToDeduct = UserDataService.TitlePublicConfiguration.AiSettings.AiRequestCurrencyAmount;

                int currentAmount = IGSUserData.UserInventory.VirtualCurrency.GetValueOrDefault(currencyCode, 0);

                int newAmount = currentAmount - amountToDeduct;

                IGSUserData.UserInventory.VirtualCurrency[currencyCode] = newAmount;

                UserDataService.VirtualCurrencyUpdatedInvoke();
            }

            return response;
        }

        // Отправка сообщения бота
        public void SendBotMessage(string message)
        {
            StartCoroutine(TypeTextWithCoroutine(message, Instantiate(_botMessagePrefab, _scrollRect.content.transform)));
            SetActivateLoading(false);

            messages.Add(new MessageAI
            {
                Role = "assistant",
                Content = message
            });

            // Сохранение истории сообщений
            SaveMessages(messages);
        }

        // Эффект печатания текста
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

        // Настройка активности кнопки отправки
        private void SetInteractableSendButton(bool interactable)
        {
            _sendButton.interactable = interactable;
        }

        // Получение текста из поля ввода
        public string GetInputMessage()
        {
            return _inputField.text;
        }

        // Очистка поля ввода
        private void ClearInput()
        {
            _inputField.text = string.Empty;
            CheckInputLength(string.Empty); // Проверить длину после очистки
        }

        // Начать прокрутку вниз
        private void StartScrollToBottom()
        {
            StartCoroutine(nameof(ScrollToTop));
        }

        // Прокрутка вверх
        private IEnumerator ScrollToTop()
        {
            yield return new WaitForEndOfFrame();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        // Активация/деактивация индикатора загрузки
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

        // Метод для отправки сообщения через префаб
        private void SendMessagePrefab(ChatMessage prefab, string message)
        {
            var userMessage = Instantiate(prefab, _scrollRect.content.transform);
            userMessage.Set(message);
        }

        // Метод для загрузки истории сообщений
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
                return true; // История сообщений загружена
            }
            return false; // История сообщений отсутствует
        }

        // Метод для сохранения сообщений в PlayerPrefs
        private void SaveMessages(List<MessageAI> messages)
        {
            string json = JsonConvert.SerializeObject(messages);
            PlayerPrefs.SetString(MESSAGE_HISTORY_KEY, json);
            PlayerPrefs.Save();
        }
    }
}
