using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TicTacToeBoardManager : NetworkBehaviour
{
    [SerializeField] private Sprite xSprite;
    [SerializeField] private Sprite oSprite;

    private Button[,] buttons = new Button[3, 3];

    public override void OnNetworkSpawn()
    {
        var cells = GetComponentsInChildren<Button>();

        int n = 0;
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                buttons[i, j] = cells[n];
                n++;

                int r = i;
                int c = j;

                buttons[i, j].onClick.AddListener(() =>
                {
                    OnCellClick(r, c);
                });
            }
        }
    }

    private void OnCellClick(int r, int c)
    {
        //If the button is clicked by host, change the sprite as X
        if(NetworkManager.Singleton.IsHost && GameManager.Instance.currentTurnIndex.Value == 0)
        {
            buttons[r, c].GetComponent<Image>().sprite = xSprite;
            buttons[r, c].enabled = false;
            ChangeSpriteClientRpc(r, c);
            CheckResult(r, c);
            GameManager.Instance.currentTurnIndex.Value = 1;
        }

        //If the button is clicked by client, change the sprite as O
        else if(!NetworkManager.Singleton.IsHost && GameManager.Instance.currentTurnIndex.Value == 1)
        {
            buttons[r, c].GetComponent<Image>().sprite = oSprite;
            buttons[r, c].enabled = false;
            CheckResult(r, c);
            ChangeSpriteServerRpc(r, c);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeSpriteServerRpc(int r, int c)
    {
        buttons[r, c].GetComponent<Image>().sprite = oSprite;
        buttons[r, c].enabled = false;
        GameManager.Instance.currentTurnIndex.Value = 0;
    }

    [ClientRpc]
    private void ChangeSpriteClientRpc(int r, int c)
    {
        buttons[r, c].GetComponent<Image>().sprite = xSprite;
        buttons[r, c].enabled = false;
    }

    private void CheckResult(int r, int c)
    {
        if(IsGameEnded(r, c))
        {
            GameManager.Instance.ShowMessage("Won");
        }
        else
        {
            if(IsGameDraw())
            {
                GameManager.Instance.ShowMessage("Draw");
            }
        }
    }

    public bool IsGameEnded(int r, int c)
    {
        Sprite clickedButtonSprite = buttons[r, c].GetComponent<Image>().sprite;

        //Checking Column
        if(buttons[0, c].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[1, c].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[2, c].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        //Checking Row
        else if(buttons[r, 0].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[r, 1].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[r, 2].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        //Checking First Diagonal
        else if(buttons[0, 0].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[1, 1].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[2, 2].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        //Checking Second Diagonal
        else if(buttons[0, 2].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[1, 1].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            buttons[2, 0].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        return false;
    }

    private bool IsGameDraw()
    {
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                if(buttons[i, j].GetComponent<Image>().sprite != xSprite && 
                    buttons[i, j].GetComponent<Image>().sprite != oSprite)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
