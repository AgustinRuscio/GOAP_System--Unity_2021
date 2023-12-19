using System;
using System.Collections.Generic;
using System.Linq;

public class GoapState
{
    public WorldState worldState;


    public GoapAction generatingAction = null;
    public int step = 0;

    #region CONSTRUCTOR
    public GoapState(GoapAction gen = null)
    {
        generatingAction = gen;
        worldState = new WorldState()
        {
            values = new Dictionary<string, bool>() // Muy importane inicializarlo en este caso
        };
    }

    public GoapState(GoapState source, GoapAction gen = null)
    {
        worldState = source.worldState.Clone();
        generatingAction = gen;
    }
    #endregion


    public override bool Equals(object obj)
    {
        var result =
            obj is GoapState other
            && other.generatingAction == generatingAction  
            && other.worldState.values.Count == worldState.values.Count
            && other.worldState.values.All(kv => kv.In(worldState.values));
        return result;
    }

    public override int GetHashCode()
    {
        return worldState.values.Count == 0 ? 0 : 31 * worldState.values.Count + 31 * 31 * worldState.values.First()
            .GetHashCode();
    }

    public override string ToString()
    {
        var str = "";
        foreach (var kv in worldState.values.OrderBy(x => x.Key))
        {
            str += (string.Format("{0:12} : {1}\n", kv.Key, kv.Value));
        }
        return ("--->" + (generatingAction != null ? generatingAction.Name : "NULL") + "\n" + str);
    }
}


//Nuestro estado de mundo
//Aca hay una mezcla de lo  anterior con lo nuevo, no necesariamente tiene que haber un diccionario aca adentro
public struct WorldState
{
    public int playerHP;
    public Dictionary<string, bool> values;

    public float _life;
    public float _maxLife;

    public int _inSightEnemies;

    public bool _treasure;

    public Weapon _weapon;
    
    //MUY IMPORTANTE TENER UN CLONE PARA NO TENER REFENCIAS A LO VIEJO

    public Kind SetRandomRarity()
    {
        Kind[] allKinds = (Kind[])Enum.GetValues(typeof(Kind));
        return allKinds[UnityEngine.Random.Range(1, allKinds.Length)];
    }
    public WorldState Clone()
    {
        return new WorldState()
        {
            playerHP = this.playerHP,
            values = this.values.ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }
}
