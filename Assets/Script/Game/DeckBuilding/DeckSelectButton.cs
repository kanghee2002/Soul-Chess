using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class DeckSelectButton : MonoBehaviour
{
    public TextMeshProUGUI deckname;
    public int deck_index;
    public void DeckSelect()
    {
        if (GetComponentInParent<LobbySceneUI>() == null)
        {
            //������ ���õǾ� �ִ� ���� ���̴� ����
            if (GetComponentInParent<PvELobbySceneUI>().SelectedDeckIndex != -1)
                GetComponentInParent<PvELobbySceneUI>().GetSelectedDeckButton().ControlShader(false);

            //���� �� ���̴� Ȱ��ȭ
            ControlShader(true);

            GetComponentInParent<PvELobbySceneUI>().SelectedDeckIndex = deck_index;

            //ī���� ���ٸ� ���⼭�� �߰��ؾ���
            GetComponentInParent<PvELobbySceneUI>().ShowSelectedDeckCardList();
        }

        else
        {
            //������ ���õǾ� �ִ� ���� ���̴� ����
            if (GetComponentInParent<LobbySceneUI>().SelectedDeckIndex != -1)
                GetComponentInParent<LobbySceneUI>().GetSelectedDeckButton().ControlShader(false);

            //���� �� ���̴� Ȱ��ȭ
            ControlShader(true);

            GetComponentInParent<LobbySceneUI>().SelectedDeckIndex = deck_index;

            //ī���� ���ٸ� ���⼭�� �߰��ؾ���
            GetComponentInParent<LobbySceneUI>().ShowSelectedDeckCardList();
        }
    }

    public void ControlShader(bool activate)
    { }
}
