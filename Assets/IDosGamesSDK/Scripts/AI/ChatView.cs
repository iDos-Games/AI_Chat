using System.Collections;
using System.Collections.Generic;
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
        private const int MIN_MESSAGE_LENGTH = 2;
        private const int MAX_MESSAGE_LENGTH = 500;
        private List<MessageAI> messages = new List<MessageAI>();

        string _welcomeMessage;

        private void Start()
        {
            SetActivateLoading(false);
            SetInteractableSendButton(true);

            if (string.IsNullOrEmpty(UserDataService.TitlePublicConfiguration.AiSettings.AiWelcomeMessage))
            {
                _welcomeMessage = "Hello, I'm a support bot, can I help you?";
            }
            else
            {
                _welcomeMessage = UserDataService.TitlePublicConfiguration.AiSettings.AiWelcomeMessage;
            }

            SendBotMessage(_welcomeMessage);

            // ���������� ����������� ����� ������
            _sendButton.onClick.AddListener(SendUserMessage);

            // �������� �� ��������� ������ � ���� �����
            _inputField.onValueChanged.AddListener(CheckInputLength);
            ClearInput();
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

        // �������� ����� ����� � ��������� ���������� ������
        private void CheckInputLength(string input)
        {
            SetInteractableSendButton(input.Length >= MIN_MESSAGE_LENGTH && input.Length <= MAX_MESSAGE_LENGTH);
        }

        // �������� ��������� ������������
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

            string aiResponse = await GetAIResponse();
            SendBotMessage(aiResponse);
        }

        // ��������� ������ AI
        private async Task<string> GetAIResponse()
        {
            var request = new AIRequest
            {
                Messages = messages
            };

            string response = await AIService.CreateThreadAndRun(request);
            return response;
        }

        // �������� ��������� ����
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
        private void SetInteractableSendButton(bool interactable)
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
            CheckInputLength(string.Empty); // ��������� ����� ����� �������
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

        // ����� ��� �������� ��������� ����� ������
        private void SendMessagePrefab(ChatMessage prefab, string message)
        {
            var userMessage = Instantiate(prefab, _scrollRect.content.transform);
            userMessage.Set(message);
        }
    }
}
