using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DonQuixote : SoulCard
{
    protected override int CardID => Card.cardIdDict["돈키호테"];

    public int standardAD;
    public int extraAD;
    private bool extraAttack = false;
    protected override void Awake()
    {
        base.Awake();
        gameObject.GetComponent<SoulCard>().OnInfuse += InfuseEffect;
    }

    public void InfuseEffect(ChessPiece chessPiece)
    {
        chessPiece.OnStartAttack += StartAttackEffect;
        chessPiece.OnEndAttack += EndAttackEffect;
    }

    public void StartAttackEffect(ChessPiece chessPiece)
    {
        if (chessPiece.AD >= standardAD)
        {
            this.InfusedPiece.AD += extraAD;
            extraAttack = true;
        }
    }

    public void EndAttackEffect(ChessPiece chessPiece)
    {
        if (extraAttack)
        {
            this.InfusedPiece.AD -= extraAD;
            extraAttack = false;
        }
    }
}
