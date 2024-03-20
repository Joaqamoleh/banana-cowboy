using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static CharacterDialogue;
using static Dialog;

public class DialogueManager : MonoBehaviour
{
    [SerializeField]
    GameObject dialogHolder;
    [SerializeField]
    Image portraitBorder;
    [SerializeField]
    Image portraitDisplay;
    [SerializeField]
    Image nameTagDisplay;
    [SerializeField]
    TMP_Text nameTextDisplay;
    [SerializeField]
    TMP_Text dialogTextDisplay;

    private Dialog _currentDialog = null;
    private StringBuilder _currentDisplayText = new StringBuilder();
    private Coroutine _typingCoroutine = null;
    private bool _typingDone = false;
    private float _animDelta = 0.0f; // For future animations on text


    private static DialogueManager s_instance;



    private void Start()
    {
        s_instance = this;
    }

    public void DisplayDialog(Dialog dialog, bool playTypingAnim = true)
    {
        SetTextColor(dialog.typeOfCharacter);

        if (_currentDialog != null)
        {
            _currentDialog.dialogFullyDisplayed = false;
        }

        _currentDialog = dialog;
        dialogHolder.SetActive(true);

        // Stop previous typing animation if it was playing
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        // Reset values
        _animDelta = 0.0f;
        _currentDisplayText.Clear();

        // Play typing if applicable
        if (playTypingAnim)
        {
            _typingDone = false;
            _typingCoroutine = StartCoroutine(Type(dialog.speakerDialog, dialog.typingSpeed));
        }
        else
        {
            _typingDone = true;
            dialog.dialogFullyDisplayed = true;
            _currentDisplayText.Append(dialog.speakerDialog);
        }

        // Display image if there is one
        if (dialog.speakerImage != null)
        {
            portraitBorder.gameObject.SetActive(true);
            portraitDisplay.sprite = dialog.speakerImage;
        } 
        else
        {
            portraitBorder.gameObject.SetActive(false);
        }

        // Display name if there is one
        if (dialog.speakerName != string.Empty)
        {
            nameTagDisplay.gameObject.SetActive(true);
            nameTextDisplay.text = dialog.speakerName;
        }
        else
        {
            nameTagDisplay.gameObject.SetActive(false);
        }
        dialogTextDisplay.text = _currentDisplayText.ToString();
    }

    private void SetTextColor(TypeOfCharacter character)
    {
        switch (character) 
        {
            case TypeOfCharacter.Orange:
                dialogTextDisplay.color = new Color(1, 172 / 255.0f, 110 / 255.0f);
                break;
            case TypeOfCharacter.Banana:
                dialogTextDisplay.color = Color.yellow;
                break;
            case TypeOfCharacter.Pete:
                dialogTextDisplay.color = new Color(255 / 255f, 182 / 255f, 155 / 255f);
                break;

        }

    }

    public bool IsTypingDone()
    {
        return _typingDone;
    }

    public void HideActiveDialog()
    {
        // Stop previous typing animation if it was playing
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        dialogHolder.SetActive(false);
    }

    public void HideDialog(Dialog dialog)
    {
        if (_currentDialog == dialog)
        {
            HideActiveDialog();
        }
    }

    IEnumerator Type(string sentence, float typingSpeed)
    {
        foreach (char letter in sentence.ToCharArray())
        {
            _currentDisplayText.Append(letter);
            dialogTextDisplay.text = _currentDisplayText.ToString();
            yield return new WaitForSeconds(typingSpeed);
        }
        _typingDone = true;
        _currentDialog.dialogFullyDisplayed = true;
    }

    public static DialogueManager Instance()
    {
        return s_instance;
    }
}

[System.Serializable]
public class Dialog
{
    public enum DialogAnimation
    {
        DEFAULT,
        WOBBLY,
    }
    public float typingSpeed = 0.02f;
    public string speakerName = string.Empty;
    [TextArea(1,15)]
    public string speakerDialog = string.Empty;
    public Sprite speakerImage = null;
    public TypeOfCharacter typeOfCharacter = TypeOfCharacter.Default;
    public enum TypeOfCharacter
    {
        Default,
        Strawberry,
        Blueberry,
        Orange,
        Banana,
        Pete
    };

    [HideInInspector]
    public bool dialogFullyDisplayed = false;
}