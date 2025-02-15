/*
using CodeStage.AntiCheat.ObscuredTypes;
using CodeStage.AntiCheat.Storage;
using IDosGames;
using IDosGames.ClientModels;
using PlayFab;
using PlayFab.ClientModels;
using System;
using TMPro;
using UnityEngine;

public class SendSystem : MonoBehaviour
{
    [SerializeField] private Chat _chat;
    [SerializeField] private ChatView _chatView;
    [SerializeField] private GameObject _shopWindow;

    public TMP_Text[] _currencyAmountTexts;

    private const int MAX_COINS_AMOUNT_BEFORE_SUBTRACTION = 5;

    private ObscuredInt _coinsAmountInInventory => UserInventory.GetVirtualCurrencyAmount(VirtualCurrencyID.CO);
    private ObscuredInt _currentCoinsAmountBeforeSubtraction;

    private ObscuredInt _totalAmountOfCoins => _coinsAmountInInventory + _currentCoinsAmountBeforeSubtraction;

    private void OnEnable()
    {
        UserInventory.InventoryUpdated += UpdateCurrencyAmountUI;
    }

    private void OnDisable()
    {
        UserInventory.InventoryUpdated -= UpdateCurrencyAmountUI;
        SaveCoinsAmountBeforeSubtraction();
    }

    private void Start()
    {
        LoadCoinsAmountBeforeSubtraction();
    }

    private void SaveCoinsAmountBeforeSubtraction()
    {
        ObscuredPrefs.SetInt("SendCoin", _currentCoinsAmountBeforeSubtraction);
    }

    private void LoadCoinsAmountBeforeSubtraction()
    {
        _currentCoinsAmountBeforeSubtraction = ObscuredPrefs.GetInt("SendCoin", MAX_COINS_AMOUNT_BEFORE_SUBTRACTION);
        UpdateCurrencyAmountUI();
    }

    public void TrySendMessage()
    {
        if (_chatView.GetInputMessage() == string.Empty)
        {
            return;
        }

        if (_currentCoinsAmountBeforeSubtraction <= 0)
        {
            if (_coinsAmountInInventory > 0)
            {
                SubtractCoins();
            }
            else
            {
                _chat.Invite();
                _shopWindow.SetActive(true);
            }
        }
        else
        {
            _chat.SendMessage();
        }
    }

    public void UpdateCurrencyAmountUI()
    {
        foreach (var currencyAmount in _currencyAmountTexts)
        {
            currencyAmount.text = $"{_totalAmountOfCoins}";
        }
    }

    public void SubtractSendCoin()
    {
        _currentCoinsAmountBeforeSubtraction--;
        SaveCoinsAmountBeforeSubtraction();
        UpdateCurrencyAmountUI();
    }

    private void SubtractCoins()
    {
        Debug.Log("Subtract coins");

        var coinsToSubtract = GetCoinsAmountToSubtract();

        ExecuteCloudScriptRequest request = new()
        {
            FunctionName = "SubtractSendCoin",
            FunctionParameter = new { value = coinsToSubtract }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnSubtractValueSuccess, OnSubtractValueError);
    }

    private int GetCoinsAmountToSubtract()
    {
        var coinsToSubtract = MAX_COINS_AMOUNT_BEFORE_SUBTRACTION;

        if (_coinsAmountInInventory < MAX_COINS_AMOUNT_BEFORE_SUBTRACTION)
        {
            coinsToSubtract = _coinsAmountInInventory;
        }

        return coinsToSubtract;
    }

    private void OnSubtractValueSuccess(ExecuteCloudScriptResult result)
    {
        if (result != null && result.FunctionResult != null)
        {
            _currentCoinsAmountBeforeSubtraction = GetCoinsAmountToSubtract();
            _chat.SendMessage();
        }
        else if (result.FunctionResult == null)
        {
            _chatView.SendInviteMessage("I have a fantastic offer! Why don't we invite your friends and get 3% Tokens from their Store spending?");
            _shopWindow.SetActive(true);
        }
        else
        {
            _chatView.SendBotMessage("Something went wrong. In Settings you can send a Bug Report");
        }

        PlayFabService.RequestUserInventory();
    }

    private void OnSubtractValueError(PlayFabError error)
    {
        _chatView.SendBotMessage("Something went wrong. In Settings you can send a Bug Report");
    }
}
*/