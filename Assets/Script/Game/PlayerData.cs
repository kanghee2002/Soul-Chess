using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using DG.Tweening;
using Unity.VisualScripting;
using System.Linq;

[Serializable]
public class PlayerData
{
    public GameBoard.PlayerColor playerColor;

    public int soulOrbs; // 자원 최대치
    public int soulEssence; // 현재 자원량

    public int maxHandCardCount = 8;    //최대 손패
    public int mulliganHandCount = 4;   //첫 손패

    public bool isRevealed = false; //감반테인 투시용

    public List<Card> deck;
    public List<Card> hand;
    public Action<Card> OnGetCard;      // Card는 새로 뽑은 카드

    public Vector2 deckPosition;

    private int exhuastionDamage = 0;

    // 게임 시작시 호출
    public void Initialize()
    {
        deckPosition = new Vector2(7.6f, -2.3f); //UI에 맞게 좌표수정

        ShuffleDeck();
        Mulligan();
    }

    // 드로우
    public bool DrawCard()
    {
        if (deck.Count <= 0)
        {
            return false;
        }
        else
        {
            Card card = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            card.FlipFront();

            if (IsHandFull())
            {
                DestroyCard(card);
            }
            else
            {
                card.transform.position = new Vector3(-5.85f, -7f, 0);
                card.transform.localScale = new Vector3(1.25f, 1.25f, 0);
                GetCard(card);
            }
            return true;
        }
    }

    public IEnumerator DrawCardWithAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        yield return CheckBlocker();

        GameObject blocker = GameBoard.instance.chessBoard.blocker;
        blocker.SetActive(true);

        if (deck.Count <= 0)
        {
            bool mySignal;
            exhuastionDamage += 1;

            if (playerColor == GameBoard.instance.playerColor)
                mySignal = true;
            else
                mySignal = false;

            GameObject exhaustionCard = UnityEngine.Object.Instantiate(GameBoard.instance.chessBoard.exhaustionCard);
            CardObject exhaustionCardObj = exhaustionCard.GetComponent<CardObject>();
            Material frameMaterial = exhaustionCard.GetComponent<Renderer>().material;
            Material illustMaterial = exhaustionCardObj.illustration.GetComponent<Renderer>().material;
            Material backMaterial = exhaustionCardObj.backSpriteRenderer.GetComponent<Renderer>().material;

            // 탈진 카드 생성
            if (mySignal)
                exhaustionCard.transform.position = new Vector3(7.9f, -2.6f, 0);
            else
            {
                exhaustionCard.transform.position = new Vector3(7.9f, 2.6f, 0);
                exhaustionCard.transform.localEulerAngles = new Vector3(0, 0, 180);
            }
            exhaustionCard.transform.localScale = new Vector3(0.8f, 0.8f, 0);

            // 페이드 효과
            yield return DOVirtual.Float(1f, -0.1f, 0.7f, (value) => {
                    frameMaterial.SetFloat("_FadeAmount", value);
                    backMaterial.SetFloat("_FadeAmount", value);
                }).WaitForCompletion();

            // 덱에서 카드 꺼내는 효과
            if (mySignal)
            {
                yield return exhaustionCard.transform.DOLocalMoveY(-7, 0.6f)
                    .SetRelative()
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        exhaustionCardObj.backSpriteRenderer.gameObject.SetActive(false);
                    })
                    .WaitForCompletion();
            }
            else
            {
                yield return exhaustionCard.transform.DOLocalMoveY(7, 0.6f)
                    .SetRelative()
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        exhaustionCardObj.backSpriteRenderer.gameObject.SetActive(false);
                    })
                    .WaitForCompletion();
            }

            yield return GameBoard.instance.chessBoard.FadeInTween().WaitForCompletion();
            
            // 카드 사용하는 것 처럼 띄우기
            exhaustionCardObj.illustration.gameObject.SetActive(true);
            exhaustionCardObj.cardNameText.gameObject.SetActive(true);
            exhaustionCardObj.descriptionText.gameObject.SetActive(true);
            exhaustionCardObj.descriptionText.text = "무작위 아군 기물에게 " + exhuastionDamage +"의 피해를 입힙니다.";
            exhaustionCard.transform.localPosition = new Vector3(-5.8f, 0f, 0);
            exhaustionCard.transform.localScale = new Vector3(1.5f, 1.5f, 0);
            illustMaterial.SetFloat("_FadeAmount", -0.1f);

            yield return new WaitForSeconds(1f);

            // 카드 fadeOut
            yield return DOVirtual.Float(-0.1f, 1f, 0.7f, (value) => {
                    float lerpedValue = Mathf.InverseLerp(-0.1f, 1f, value);
                    exhaustionCardObj.cardNameText.alpha = 1 - lerpedValue;
                    exhaustionCardObj.descriptionText.alpha = 1 - lerpedValue;
                    illustMaterial.SetFloat("_FadeAmount", value);
                    frameMaterial.SetFloat("_FadeAmount", value);
                });
            
            GameObject projectileObj = UnityEngine.Object.Instantiate(GameBoard.instance.chessBoard.exhaustionProjectile, exhaustionCard.transform);

            // 이펙트 발사
            yield return DOVirtual.DelayedCall(0.4f, () => {    
                List<ChessPiece> targetList = GameBoard.instance.gameData.pieceObjects.Where(obj => obj.pieceColor == playerColor).ToList();
                ChessPiece targetPiece = targetList[SynchronizedRandom.Range(0, targetList.Count())];

                projectileObj.transform.DOMove(targetPiece.transform.position, 0.7f).SetEase(Ease.InOutQuint).OnComplete(() => {
                    targetPiece.MinusHP(exhuastionDamage);
                    if (targetPiece.isAlive)
                    {
                        GameManager.instance.soundManager.PlaySFX("Attack");
                        GameBoard.instance.chessBoard.AttackedAnimation(targetPiece);
                    }
                    else
                    {
                        GameManager.instance.soundManager.PlaySFX("Destroy");
                        targetPiece.GetComponent<Animator>().SetTrigger("killedTrigger");
                        targetPiece.MakeAttackedEffect();
                    }
                    UnityEngine.Object.Destroy(projectileObj);
                });
            }).WaitForCompletion();

            yield return new WaitForSeconds(0.8f);
            yield return GameBoard.instance.chessBoard.FadeOutTween();

            UpdateHandPosition();
            yield break;
        }
        else
        {
            GameManager.instance.soundManager.PlaySFX("Draw", volume: 3f);
            Card card = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
 
            yield return card.transform.DOLocalMoveY(-7, 0.6f)
                .SetRelative()
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    card.FlipFront();
                })
                .WaitForCompletion();

            if (playerColor == GameBoard.instance.playerColor)
            {
                card.transform.localPosition = new Vector3(-5f, 3.5f, 0);
                card.transform.localScale = new Vector3(2.5f, 2.5f, 0);
            }

            yield return new WaitForSeconds(1f);

            if (IsHandFull())
            {
                UpdateHandPosition();
                CardObject cardobj = card.GetComponent<CardObject>();
                GameObject costCircle = cardobj.costText.transform.parent.gameObject;
                Material frameMaterial = card.GetComponent<Renderer>().material;
                Material illustMaterial = cardobj.illustration.GetComponent<Renderer>().material;
                    
                if (card is SoulCard)
                {
                    cardobj.ADCircle.SetActive(false);
                    cardobj.HPCircle.SetActive(false);
                }
                cardobj.canUseEffectRenderer.gameObject.SetActive(false);
                cardobj.backSpriteRenderer.gameObject.SetActive(false);
                cardobj.typeBackground.SetActive(false);
                cardobj.typeImage.SetActive(false);
                costCircle.SetActive(false);

                yield return DOVirtual.Float(-0.1f, 1f, 0.4f, (value) => {
                    frameMaterial.SetFloat("_FadeAmount", value);
                    illustMaterial.SetFloat("_FadeAmount", value);
                }).WaitForCompletion();

                yield return DOVirtual.Float(1f, 0f, 0.4f, (value) => {
                    cardobj.cardNameText.alpha = value;
                    cardobj.descriptionText.alpha = value;
                    
                    float lerpedValue = Mathf.Lerp(0.5f, 1f, value);
                    frameMaterial.SetFloat("_FadeBurnWidth", lerpedValue);
                    illustMaterial.SetFloat("_FadeBurnWidth", lerpedValue);
                }).SetEase(Ease.Linear).WaitForCompletion();
                DestroyCard(card);
                blocker.SetActive(false);
            }
            else
            {
                card.transform.localScale = new Vector3(1.25f, 1.25f, 0);

                if (playerColor == GameBoard.instance.playerColor)
                {
                    card.transform.position = new Vector3(-5.85f, -7f, 0);
                }
                else
                {
                    card.transform.position = new Vector3(-5.85f, 7f, 0);
                }
                GetCard(card);
                blocker.SetActive(false);
            }
        }
    }

    private IEnumerator CheckBlocker()
    {
        while (true)
        {
            if (GameBoard.instance.chessBoard.blocker.activeSelf)
            {
                Debug.Log("Blocker Exist");
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            else
            {
                break;
            }
        }
    }

    private bool IsHandFull()
    {
        return hand.Count >= maxHandCardCount;
    }
    public Action UpdateHandPosition;
    /*
        public void UpdateHandPosition()
        {
            for (int i = 0; i < hand.Count; i++)
            {
                hand[i].transform.position = new Vector3(0.5f * i - 8, -3.75f, -0.1f * i); //UI에 맞게 좌표수정
            }
        }
    */
    public bool TryAddCardInHand(Card cardInstance)
    {
        if (IsHandFull())
        {
            return false;
        }
        else
        {
            GetCard(cardInstance);
            return true;
        }
    }

    // 핸드에 있는 카드 삭제 (cardInstance: 핸드에서 지정되어야 함, 프리팹 X)
    public bool TryRemoveCardInHand(Card cardInstance)
    {
        if (cardInstance.handIndex != -1)
        {
            hand.RemoveAt(cardInstance.handIndex);
            UpdateHandPosition();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void RemoveHandCards()
    {
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            DestroyCard(hand[i]);
        }
        hand.Clear();
    }

    public void RemoveDeckCards()
    {
        for (int i = deck.Count - 1; i >= 0; i--)
        {
            DestroyCard(deck[i]);
        }
        deck.Clear();
    }

    // 카드 획득시 실행
    public void GetCard(Card cardInstance)
    {
        hand.Add(cardInstance);
        if (cardInstance.isMine)
            cardInstance.FlipFront();
        else
            cardInstance.FlipBack();
        cardInstance.GetComponent<SortingGroup>().sortingOrder = hand.Count - 1;

        UpdateHandPosition();

        OnGetCard?.Invoke(cardInstance);
    }

    private void DestroyCard(Card cardInstance)
    {
        cardInstance.Destroy();
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int j = Random.Range(i, deck.Count);
            Card tmp = deck[i];
            deck[i] = deck[j];
            deck[j] = tmp;
        }
    }

    private void Mulligan()
    {
        for (int i = 0; i < mulliganHandCount; i++)
        {
            DrawCard();
        }

        foreach (Card card in hand)
        {
            //card.isMulligan = true;
        }
    }

    public void ChangeMulligan(Card cardInstance)
    {
        int index = Random.Range(0, deck.Count + 1);
        deck.Insert(index, cardInstance);
        cardInstance.FlipBack();
        cardInstance.transform.position = deckPosition;
        //cardInstance.isMulligan = false;

        TryRemoveCardInHand(cardInstance);

        DrawCard();
    }



    public int spellDamageIncrease = 0;
    public int spellDamageCoefficient = 1;

    public void SpellAttack(ChessPiece targetPiece, int damage)
    {
        targetPiece.SpellAttacked((damage + spellDamageIncrease) * spellDamageCoefficient);
    }

    public void UpdateOneCardPosition(Card targetCard)
    {
        float anchor_x;

        if (hand.Count == 0)
            anchor_x = 0;
        else if (hand.Count % 2 == 0)
            anchor_x = -(hand.Count / 2f - 0.5f) * 0.5f;
        else
            anchor_x = -(hand.Count / 2f) * 0.5f;

        targetCard.gameObject.transform.localPosition = new Vector3(anchor_x + 0.5f * targetCard.handIndex, 0, -0.1f * targetCard.handIndex);
    }

    public void RemoveHandCardEffect()
    {
        foreach (var card in hand)
        {
            card.GetComponent<CardObject>().canUseEffectRenderer.material.SetFloat("_Alpha", 0f);
        }
    }

    public bool CheckAllCardUnAvailable()
    {
        bool unAvailableSignal = true;

        foreach (var objCard in hand.ToList())
        {
            if (CheckCardUseAvailable(objCard))
                unAvailableSignal = false;
        }

        return unAvailableSignal;
    }

    public bool CheckCardUseAvailable(Card objCard)
    {
        bool availableSignal = false;

        if (objCard.cost <= soulEssence)
        {
            availableSignal = true;

            if (objCard.EffectOnCardUsed is TargetingEffect effect && !effect.isAvailable(playerColor))
            {
                availableSignal = false;
            }
        }

        return availableSignal;
    }
}
