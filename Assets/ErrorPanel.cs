using System.Collections;
using TMPro;
using UnityEngine;

public class ErrorPanel : MonoBehaviour{

    [SerializeField] GameObject errorPanel;
    [SerializeField] TMP_Text errorText;

    int timer = 0;

    public void DisplayError(string error) {
        if(timer > 0) {
            timer = 5;
        }
        else {
            errorPanel.SetActive(true);
            StartCoroutine(RemoveError());
        }
        errorText.text = error;
    }

    IEnumerator RemoveError() {
        timer = 5;
        while (timer > 0) {
            timer--;
            yield return new WaitForSeconds(1);
        }

        errorPanel.SetActive(false);
    }

}
