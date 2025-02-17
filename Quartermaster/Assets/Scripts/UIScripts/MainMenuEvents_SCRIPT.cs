// Code help from: https://www.youtube.com/watch?v=_jtj73lu2Ko&ab_channel=SasquatchBStudios
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuEvents : MonoBehaviour
{
     private UIDocument _document;
     private Button _button;

     private void Awake()
     {
          _document = GetComponent<UIDocument>();
          _button = _document.rootVisualElement.Q("StartGameButton") as Button;
          _button.RegisterCallback<ClickEvent>(OnPlayGameClick);
     }

     private void OnDisable()
     {
          _button.UnregisterCallback<ClickEvent>(OnPlayGameClick);
     }

     private void OnPlayGameClick(ClickEvent evt)
     {
          // Transition to the game scene
          SceneManager.LoadScene("SampleScene");
     }
}
