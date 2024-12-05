using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private List<MessageAI> messages = new List<MessageAI>();

        private async void Start()
        {
            SetActivateLoading(false);
            SetInteractableSendButton(true);

            // Первое сообщение от ассистента
            string initialMessage = "Hello, I'm a support bot, can I help you?";
            SendBotMessage(initialMessage);
        }

        private void OnEnable()
        {
            SetActivateLoading(false);
            SetInteractableSendButton(true);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        // Отправка сообщения пользователя
        public async void SendUserMessage()
        {
            string message = GetInputMessage();
            SendMessagePrefab(_userMessagePrefab, message);
            ClearInput();
            StartScrollToBottom();
            SetActivateLoading(true);

            messages.Add(new MessageAI
            {
                Role = "user",
                Content = message
            });

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
    }
}
