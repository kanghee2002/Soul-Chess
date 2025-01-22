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
        //������ ���õǾ� �ִ� ���� ���̴� ����
        GetComponentInParent<LobbySceneUI>().GetSelectedDeckButton().ControlShader(false);

        //���� �� ���̴� Ȱ��ȭ
        ControlShader(true);

        //ī���� ���ٸ� ���⼭�� �߰��ؾ���
        ShowCardList();

        GetComponentInParent<LobbySceneUI>().SelectedDeckIndex = deck_index;
    }

    public void ControlShader(bool activate)
    { }

    public void ShowCardList()
    { }
}
