using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private void Start()
        {
            LoadThreads();  // �������� ����������� ������ ��� ������  
            if (threads.Count == 0)
            {
                CreateNewThread("thread_abc123"); // �������� ������ �����, ���� �� ������ ���  
            }
            else
            {
                // ������������ �� ������ ��������� ����  
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

        // �������� ��������� ������������  
        public void SendUserMessage(string message)
        {
            var userMessage = Instantiate(_userMessagePrefab, _scrollRect.content.transform);
            userMessage.Set(message);
            ClearInput();
            StartScrollToBottom();
            SetActivateLoading(true);

            AddMessageToCurrentThread("user", message, messageId: Guid.NewGuid().ToString());
        }

        // �������� ��������� ����  
        public void SendBotMessage(string message)
        {
            var botMessage = Instantiate(_botMessagePrefab, _scrollRect.content.transform);
            StartCoroutine(TypeTextWithCoroutine(message, botMessage));
            SetActivateLoading(false);

            AddMessageToCurrentThread("assistant", message, messageId: Guid.NewGuid().ToString());
        }

        // �������� ���������������� ���������  
        public void SendInviteMessage(string message)
        {
            var botMessage = Instantiate(_inviteMessagePrefab, _scrollRect.content.transform);
            StartCoroutine(TypeTextWithCoroutine(message, botMessage));
            SetActivateLoading(false);

            AddMessageToCurrentThread("assistant", message, messageId: Guid.NewGuid().ToString());
        }

        // ������ ��������� ������  
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

        // ��������� ���������� ������ ��������  
        public void SetInteractableSendButton(bool interactable)
        {
            _sendButton.interactable = interactable;
        }

        // ��������� ������ �� ���� �����  
        public string GetInputMessage()
        {
            return _inputField.text;
        }

        // ������� ���� �����  
        private void ClearInput()
        {
            _inputField.text = string.Empty;
        }

        // ������ ��������� ����  
        private void StartScrollToBottom()
        {
            StartCoroutine(nameof(ScrollToTop));
        }

        // ��������� �����  
        private IEnumerator ScrollToTop()
        {
            yield return new WaitForEndOfFrame();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        // ���������/����������� ���������� ��������  
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

        // ���������� ����  
        public void Refresh()
        {
            // ������ ��������� ��������  
            SetActivateLoading(false);

            // ������� ��� ������������ ��������� �� ����������� ScrollRect  
            foreach (Transform child in _scrollRect.content)
            {
                Destroy(child.gameObject);
            }

            // ���� ���� ������� ����, ���������� ��� ���������  
            if (currentThread != null)
            {
                foreach (var message in currentThread.messages)
                {
                    DisplayMessage(message);
                }
            }
        }

        // �������� ������ �����  
        public void CreateNewThread(string threadId)
        {
            currentThread = new ChatThreadModel
            {
                id = threadId,
                created_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            threads[threadId] = currentThread;
            SaveThreads();
            Refresh();
        }

        // ������������ ����� �������  
        public void SwitchThread(string threadId)
        {
            if (threads.ContainsKey(threadId))
            {
                currentThread = threads[threadId];
                Refresh();
            }
        }

        // ���������� ��������� � ������� ����  
        private void AddMessageToCurrentThread(string role, string message, string messageId)
        {
            // �������� ����� ������ ���������  
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
                        text = new ChatMessageText
                        {
                            value = message
                        }
                    }
                },
                assistant_id = role == "assistant" ? "asst_abc" : string.Empty,
                run_id = role == "assistant" ? "run_abc" : string.Empty
            };

            // ���������� ��������� � ������ ��������� �������� �����  
            currentThread.messages.Add(chatMessage);

            // ���������� ����������� ������  
            SaveThreads();

            // ����������� ������ ���������  
            DisplayMessage(chatMessage);
        }

        // ����������� ��������� � UI  
        private void DisplayMessage(ChatMessageModel message)
        {
            // ����� ������� � ����������� �� ���� (������������ ��� ���������)  
            var chatMessage = message.role == "user" ? Instantiate(_userMessagePrefab, _scrollRect.content.transform) : Instantiate(_botMessagePrefab, _scrollRect.content.transform);

            // ��������� ������ ���������  
            chatMessage.Set(message.content[0].text.value);

            // ��������� ����  
            StartScrollToBottom();
        }

        // ���������� ������ � PlayerPrefs  
        private void SaveThreads()
        {
            // ������������ ������� ������ � JSON ������ � ���������� � � �������������� PlayerPrefs  
            string json = JsonUtility.ToJson(new Serialization<Dictionary<string, ChatThreadModel>>(threads));
            PlayerPrefs.SetString("ChatThreads", json);
            PlayerPrefs.Save();
        }

        // �������� ������ �� PlayerPrefs  
        private void LoadThreads()
        {
            // �������� JSON ������ �� PlayerPrefs � �������������� � ������� � ������� ������  
            string json = PlayerPrefs.GetString("ChatThreads", string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                threads = JsonUtility.FromJson<Serialization<Dictionary<string, ChatThreadModel>>>(json).target;
            }
        }
    }
}
