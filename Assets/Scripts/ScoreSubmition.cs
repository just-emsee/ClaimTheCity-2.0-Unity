using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreSubmition : MonoBehaviour
{
    [SerializeField] Button submitButton;
    [SerializeField] TMP_InputField inputField;
    double? score = null;


    private void Start() {
        submitButton.onClick.AddListener(ButtonClicked);
        inputField.onValueChanged.AddListener(InputValueChanged);
    }
    private void ButtonClicked() {
        if (score == null) return;
        submitButton.interactable = false;
        MainNetworking.Instance.SubmitScore(score.Value, UIManager.Instance.CurrentOpenTask());
        inputField.text = "";
    }

    private void InputValueChanged(string value) {
        if (value.Length <= 0) {
            score = null;
            submitButton.interactable = false;
            return;
        }
        try {
            score = double.Parse(value);
            submitButton.interactable = true;
        } catch {
            submitButton.interactable = false;
        }
        
    }
}
