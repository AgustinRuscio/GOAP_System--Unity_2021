using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour
{
    private Rarity _rarity;

    public Rarity Rarity => _rarity;

    public void SetRarity(Rarity newRarity) => _rarity = newRarity;
}

public enum Rarity
{
    normal, 
    rare,
    legendary,
    ultraMax
}