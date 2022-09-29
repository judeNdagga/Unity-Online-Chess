using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; }

    [SerializeField] private Animator menuAnimator;

    private void Awake(){
        Instance = this;
    }

    //Buttons
    public void OnLocalGameButton(){
        Debug.Log("OnLocalGameButton");
        menuAnimator.SetTrigger("InGameMenu");
    }

    public void OnOnlineGameButton(){
        Debug.Log("OnOnlineGameButton"); 
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnOnlineHostButton(){
        Debug.Log("OnOnlineHostButton");
        menuAnimator.SetTrigger("HostMenu");
    }

    public void OnOnlineConnectButton(){
        Debug.Log("OnOnlineConnectButton");
    }

    public void OnOnlineBackButton(){
        Debug.Log("OnOnlineBackButton"); 
        menuAnimator.SetTrigger("StartMenu");
    }

    public void OnHostBackButton(){
        Debug.Log("OnHostGameButton");
        menuAnimator.SetTrigger("OnlineMenu");
    }
}
