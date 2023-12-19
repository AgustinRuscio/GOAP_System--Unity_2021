using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour
{
    private Kind _kind;

    public Kind Type => _kind;

    public void SetKind(Kind _newKind) => _kind = _newKind;
}

public enum Kind
{
    Mull,
    normal, 
    rare,
    legendary,
    ultraMax
    
}