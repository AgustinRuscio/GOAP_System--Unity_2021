using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Random = System.Random;

public class Planero : MonoBehaviour 
{
	private readonly List<Tuple<Vector3, Vector3>> _debugRayList = new List<Tuple<Vector3, Vector3>>();

	private void Start ()
    {
		StartCoroutine(Plan());
	}
	
    private void Check(Dictionary<string, bool> state, ItemType type) {

		var items = Navigation.instance.AllItems();
		var inventories = Navigation.instance.AllInventories();
		var floorItems = items.Except(inventories);//devuelve una coleccion como la primera pero removiendo los que estan en la segunda
		var item = floorItems.FirstOrDefault(x => x.type == type);
		var here = transform.position;
		state["accessible" + type.ToString()] = item != null && Navigation.instance.Reachable(here, item.transform.position, _debugRayList);

		var inv = inventories.Any(x => x.type == type);
		state["otherHas" + type.ToString()] = inv;

		state["dead" + type.ToString()] = false;
	}

    private IEnumerator Plan() {
		yield return new WaitForSeconds(0.2f);

		var observedState = new Dictionary<string, bool>();
		
		var nav = Navigation.instance;//Consigo los items
		var floorItems = nav.AllItems();
		var inventory = nav.AllInventories();
		var everything = nav.AllItems().Union(nav.AllInventories());// .Union() une 2 colecciones sin agregar duplicados(eso incluye duplicados en la misma coleccion)

        //Chequeo los booleanos para cada Item, generando mi modelo de mundo (mi diccionario de bools) en ObservedState
		Check(observedState, ItemType.Key);
		Check(observedState, ItemType.Entity);
		Check(observedState, ItemType.Mace);
		Check(observedState, ItemType.PastaFrola);
		Check(observedState, ItemType.Door);
		
        var actions = CreatePossibleActionsList();


        GoapState initial = new GoapState();
        initial.worldState = new WorldState()
        {
	        _inSightEnemies = 0,
	        _maxLife = 100,
	        _life = 100,
	        _treasure = false,
	        _weapon = null
        };

        //initial.worldState.values = observedState; //le asigno los valores actuales, conseguidos antes
		//initial.worldState.values["doorOpen"] = false; //agrego el bool "doorOpen"

		
		Debug.Log("InSightEnemies" + " ---> " + initial.worldState._inSightEnemies);
		Debug.Log("MaxLife" + " ---> " + initial.worldState._maxLife);
		Debug.Log("Life" + " ---> " + initial.worldState._life);
		Debug.Log("Treasure" + " ---> " + initial.worldState._treasure);
		Debug.Log("_weapon" + " ---> " + initial.worldState._weapon);
		
        //foreach (var item in initial.worldState.values)
        //{
        //    Debug.Log(item.Key + " ---> " + item.Value);
        //}

        GoapState goal = new GoapState();
        goal.worldState = new WorldState()
        {
			_inSightEnemies = 0,
			//_life = IsHeal(goal), //Tiene que ser mas que la mitad de maxlife
			_treasure = true,
			//_weapon =  Kind.normal//Tiene que ser
        };
        
        //goal.values["has" + ItemType.Key.ToString()] = true;
        //goal.worldState.values["has" + ItemType.PastaFrola.ToString()] = true;
        //goal.values["has"+ ItemType.Mace.ToString()] = true;
        //goal.values["dead" + ItemType.Entity.ToString()] = true;}


        Func<GoapState, float> heuristc = (curr) =>
        {
            int count = 0;
            string key = "has" + ItemType.PastaFrola.ToString();
            if (!curr.worldState.values.ContainsKey(key) || !curr.worldState.values[key])
                count++;
            if (curr.worldState.playerHP <= 45)
                count++;
            return count;
        };

        Func<GoapState, bool> objectice = (curr) =>
         {
             string key = "has" + ItemType.PastaFrola.ToString();
             return curr.worldState.values.ContainsKey(key) && curr.worldState.values["has" + ItemType.PastaFrola.ToString()]
                    && curr.worldState.playerHP > 45;
         };




        var actDict = new Dictionary<string, ActionEntity>() {
			  { "Kill"	, ActionEntity.Kill }
			, { "Pickup", ActionEntity.PickUp }
			, { "Open"	, ActionEntity.Open }
		};

		var plan = Goap.Execute(initial,null, objectice, heuristc, actions);

		if(plan == null)
			Debug.Log("Couldn't plan");
		else {
			GetComponent<Guy>().ExecutePlan(
				plan
				.Select(a => 
                {
                    Item i2 = everything.FirstOrDefault(i => i.type == a.item);
                    if (actDict.ContainsKey(a.Name) && i2 != null)
                    {
                        return Tuple.Create(actDict[a.Name], i2);
                    }
                    else
                    {
                        return null;
                    }
				}).Where(a => a != null)
				.ToList()
			);
		}
	}


    private bool IsHeal(GoapState state)
    {
	    return state.worldState._life> (state.worldState._maxLife *.5f);
    }
    
    private List<GoapAction> CreatePossibleActionsList()
    {
	     return new List<GoapAction>()
	     {
	          new GoapAction("Escape")
		          .SetCost(4f)
		          .Pre((gs) =>
		          {
			          return (gs.worldState._inSightEnemies > 0);
			          
		          })
		          .Effect((gs) =>
		          {
			          gs.worldState._inSightEnemies = 0;
			          return gs;
		          })

	          , new GoapAction("Fight")
		          .SetCost(5f)
		          .SetItem(ItemType.Entity)
		          .Pre((gs) =>
		          {
			          return (gs.worldState._inSightEnemies > 0) &&
			                 (gs.worldState._weapon != null) &&
			                 (gs.worldState._life > gs.worldState._maxLife * .3f);
		          })
		          .Effect(gs =>
		          {
			          gs.worldState._inSightEnemies = 0;
			          gs.worldState._life = gs.worldState._maxLife * .3f;
			          return gs;
		          })


	         , new GoapAction("Pickup") //Tesoro
	             .SetCost(3f)
	             .SetItem(ItemType.Treasure)
	             .Pre(gs =>
	             {
		             return gs.worldState._inSightEnemies == 0 &&
		                    gs.worldState._treasure == false;
	             })
	             .Effect(gs =>
	             {
		             gs.worldState._inSightEnemies = 2;
		             gs.worldState._treasure = true;
		             return gs;
	             })

	         , new GoapAction("Pickup") //Weapon
	             .SetCost(2f)
	             .SetItem(ItemType.WeaponChest)
	             
	             .Pre(gs =>
	             {
		             return gs.worldState._inSightEnemies == 0;
	             })
	             .Effect(gs =>
	             {
		             gs.worldState._inSightEnemies = 1;
		             gs.worldState._weapon.SetKind( gs.worldState.SetRandomRarity()); // poner aca el random
		             return gs;
	             })

	         , new GoapAction("Heal")
	             .SetCost(5f)
	             .SetItem(ItemType.Heal)
	             .Pre(gs =>
	             {
		             return gs.worldState._inSightEnemies == 0 &&
		                    gs.worldState._life < gs.worldState._maxLife;
	             })
	             .Effect(gs =>
	             {
		             gs.worldState._life = gs.worldState._maxLife;
		             return gs;
	             })
	     };
    }

	 void OnDrawGizmos()
	 {
		 Gizmos.color = Color.cyan;
		 foreach(var t in _debugRayList)
	     {
			 Gizmos.DrawRay(t.Item1, (t.Item2-t.Item1).normalized);
			 Gizmos.DrawCube(t.Item2+Vector3.up, Vector3.one*0.2f);
		 }
    }
}
