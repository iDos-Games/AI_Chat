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
        private Dictionary<string, ChatThreadModel> threads = new Dictionary<string, ChatThreadModel>();
        private ChatThreadModel currentThread;
        public Dictionary<string, ChatThreadModel> Threads => threads;

        private async void Start()
        {
            LoadThreads();
            if (threads.Count == 0)
            {
                await CreateNewThread();  // Создание нового треда через AIService
            }
            else
            {
                SwitchThread(threads.Values.First().id);
            }
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
        public async void SendUserMessage(string message)
        {
            SendMessagePrefab(_userMessagePrefab, message);
            ClearInput();
            StartScrollToBottom();
            SetActivateLoading(true);

            AddMessageToCurrentThread("user", message, messageId: Guid.NewGuid().ToString());

            // Отправляем сообщение сервису AI
            var aiRequest = new AIRequest
            {
                MessageContent = message,
            };
            string aiResponse = await AIService.CreateMessage(aiRequest);
            SendBotMessage(aiResponse);
        }

        // Отправка сообщения бота
        public void SendBotMessage(string message)
        {
            StartCoroutine(TypeTextWithCoroutine(message, Instantiate(_botMessagePrefab, _scrollRect.content.transform)));
            SetActivateLoading(false);
            AddMessageToCurrentThread("assistant", message, messageId: Guid.NewGuid().ToString());
        }

        // Отправка пригласительного сообщения
        public void SendInviteMessage(string message)
        {
            StartCoroutine(TypeTextWithCoroutine(message, Instantiate(_inviteMessagePrefab, _scrollRect.content.transform)));
            SetActivateLoading(false);
            AddMessageToCurrentThread("assistant", message, messageId: Guid.NewGuid().ToString());
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

        // Обновление чата
        public async void Refresh()
        {
            SetActivateLoading(false);

            foreach (Transform child in _scrollRect.content)
            {
                Destroy(child.gameObject);
            }

            if (currentThread != null)
            {
                foreach (var message in currentThread.messages)
                {
                    DisplayMessage(message);
                }
            }
            else
            {
                await CreateNewThread();
            }
        }

        // Создание нового треда
        public async Task CreateNewThread()
        {
            string threadId = await AIService.CreateThread();
            currentThread = new ChatThreadModel
            {
                id = threadId,
                created_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            threads[threadId] = currentThread;
            SaveThreads();
            Refresh();
        }

        // Переключение между тредами
        public void SwitchThread(string threadId)
        {
            if (threads.ContainsKey(threadId))
            {
                currentThread = threads[threadId];
                Refresh();
            }
        }

        // Добавление сообщения в текущий тред
        private void AddMessageToCurrentThread(string role, string message, string messageId)
        {
            var chatMessage = new ChatMessageModel
            {
                id = messageId,
                created_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                thread_id = currentThread.id,
                role = role,
                content = new List<ChatMessageContent>
                {
                    new ChatMessageContent
                    {
                        type = "text",
                        text = new ChatMessageText { value = message }
                    }
                },
                assistant_id = role == "assistant" ? "asst_abc" : string.Empty,
                run_id = role == "assistant" ? "run_abc" : string.Empty
            };
            currentThread.messages.Add(chatMessage);
            SaveThreads();
            DisplayMessage(chatMessage);
        }

        // Отображение сообщения в UI
        private void DisplayMessage(ChatMessageModel message)
        {
            var chatMessage = message.role == "user"
                ? Instantiate(_userMessagePrefab, _scrollRect.content.transform)
                : Instantiate(_botMessagePrefab, _scrollRect.content.transform);

            chatMessage.Set(message.content[0].text.value);
            StartScrollToBottom();
        }

        // Сохранение тредов в PlayerPrefs
        private void SaveThreads()
        {
            string json = JsonUtility.ToJson(new Serialization<Dictionary<string, ChatThreadModel>>(threads));
            PlayerPrefs.SetString("ChatThreads", json);
            PlayerPrefs.Save();
        }

        // Загрузка тредов из PlayerPrefs
        private void LoadThreads()
        {
            string json = PlayerPrefs.GetString("ChatThreads", string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                threads = JsonUtility.FromJson<Serialization<Dictionary<string, ChatThreadModel>>>(json).target;
            }
        }

        // Метод для отправки сообщения через префаб
        private void SendMessagePrefab(ChatMessage prefab, string message)
        {
            var userMessage = Instantiate(prefab, _scrollRect.content.transform);
            userMessage.Set(message);
        }
    }
}
