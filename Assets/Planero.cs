using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Planero : MonoBehaviour 
{
	private readonly List<Tuple<Vector3, Vector3>> _debugRayList = new List<Tuple<Vector3, Vector3>>();

	public Transform test;
	public Weapon wtest;
	
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
		Check(observedState, ItemType.Treasure);
		Check(observedState, ItemType.Entity);
		Check(observedState, ItemType.WeaponChest);
		Check(observedState, ItemType.Heal);
		Check(observedState, ItemType.Escape);
		
        var actions = CreatePossibleActionsList();


        GoapState initial = new GoapState();
        initial.worldState = new WorldState()
        {
	        _inSightEnemies = 0,
	        
	        _maxLife = 100,
	        _life = 100,
	        _isHealthy = true,
	        
	        _treasure = false,
	        _hasWeapon = false,
	        
	        values = new Dictionary<string, bool>()
        };

        initial.worldState.values = observedState; //le asigno los valores actuales, conseguidos antes
		initial.worldState.values["canEscape"] = false;
		initial.worldState.values["has" + ItemType.WeaponChest.ToString()] = initial.worldState._hasWeapon;
		initial.worldState.values["has" + ItemType.Treasure.ToString()] = initial.worldState._treasure;
		initial.worldState.values["isHealthy"] = initial.worldState._life > (initial.worldState._maxLife *.5f);
		
		
		Debug.Log("Initial Node");
		Debug.Log("InSightEnemies" + " ---> " + initial.worldState._inSightEnemies);
		Debug.Log("MaxLife" + " ---> " + initial.worldState._maxLife);
		Debug.Log("Life" + " ---> " + initial.worldState._life);
		Debug.Log("Treasure" + " ---> " + initial.worldState._treasure);
		Debug.Log("isHealthy " + " ---> " + initial.worldState._isHealthy);
		Debug.Log("_weapon" + " ---> " + initial.worldState._hasWeapon);
		Debug.Log("End of Initial Node");
		Debug.Log("----------------------------");
		
        foreach (var item in initial.worldState.values)
        {
            Debug.Log(item.Key + " ---> " + item.Value);
        }

		Debug.Log("----------------------------");
        GoapState goal = new GoapState();
        
        goal.worldState = new WorldState()
        {
			_inSightEnemies = 0,
			
			_isHealthy = true,
			_life = 100,
			_maxLife = 100,
			
			_treasure = true,
			_hasWeapon = true,
			
			values = new Dictionary<string, bool>()
        };
        
        goal.worldState.values["has" + ItemType.WeaponChest.ToString()] = goal.worldState._hasWeapon;
        goal.worldState.values["has" + ItemType.Treasure.ToString()] = goal.worldState._treasure;
        goal.worldState.values["isHealthy"] = goal.worldState._life > (goal.worldState._maxLife *.5f);
        goal.worldState.values["canEscape"] = false;
        
        
        //goal.values["has" + ItemType.Key.ToString()] = true;
        //goal.worldState.values["has" + ItemType.PastaFrola.ToString()] = true;
        //goal.values["has"+ ItemType.Mace.ToString()] = true;
        //goal.values["dead" + ItemType.Entity.ToString()] = true;}


        Func<GoapState, float> heuristc = (curr) =>
        {
            int count = 0;

            if(curr.worldState.values["canEscape"])
	            count++;
           if(!curr.worldState.values["isHealthy"])
           		count++;
           if (!curr.worldState.values["has" + ItemType.Treasure.ToString()])
	           count++;
           if (!curr.worldState.values["has" + ItemType.WeaponChest.ToString()])
	           count++;
            
            //if (curr.worldState._weapon == null
            //    || curr.worldState._weapon.Rarity == Rarity.normal )
	            //count++;
            return count;
        };

        Func<GoapState, bool> objectice = (curr) =>
        {
	        return !curr.worldState.values["canEscape"] &&
	               curr.worldState.values["isHealthy"] &&
	               curr.worldState.values["has" + ItemType.WeaponChest.ToString()] &&
	               curr.worldState.values["has" + ItemType.Treasure.ToString()];
        };
        
        var actDict = new Dictionary<string, Actions>() {
			  { "Escape", Actions.Escape }
			, { "PickUp", Actions.PickUp }
			, { "Fight"	, Actions.Fight }
			, { "Heal"	, Actions.Heal }
		};
        
		var plan = Goap.Execute(initial,goal, objectice, heuristc, actions);

		if(plan == null)
			Debug.Log("Couldn't plan");
		else {
			GetComponent<Player>().ExecutePlan(
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
    
    //private bool IsRarityOkey(GoapState state)
    //{
	//    return state.worldState._weapon != null &&
	//           state.worldState._weapon.Rarity != Rarity.normal;
    //}
    
    private List<GoapAction> CreatePossibleActionsList()
    {
	     return new List<GoapAction>()
	     {
	          new GoapAction("Fight")
		          .SetCost(5f)
		          .SetItem(ItemType.Entity)
		          .Pre((gs) =>
		          {
			          return gs.worldState._inSightEnemies == 1 &&
			                 gs.worldState.values["canEscape"] &&
			                 
			                 gs.worldState._hasWeapon &&
			                 gs.worldState.values.ContainsKey("has" + ItemType.WeaponChest.ToString()) &&
			                 gs.worldState.values["has" + ItemType.WeaponChest.ToString()]&&
			                 
			                 gs.worldState._isHealthy &&
			                 gs.worldState.values["isHealthy"] &&
			                 
			                 gs.worldState.values.ContainsKey("accessible" + ItemType.Entity.ToString()) &&
			                 !gs.worldState.values["dead" + ItemType.Entity.ToString()];
		          })
		          .Effect(gs =>
		          {
			          gs.worldState._inSightEnemies = 0;
			          gs.worldState.values["canEscape"] = gs.worldState._inSightEnemies == 0; 
			          
			          gs.worldState._life = gs.worldState._maxLife * .3f;
			          gs.worldState._isHealthy =  gs.worldState._life > (gs.worldState._maxLife *.5f);
			          gs.worldState.values["isHealthy"] = gs.worldState._life > (gs.worldState._maxLife *.5f);
			          
			          gs.worldState.values["dead" + ItemType.Entity.ToString()] = true;
			          gs.worldState.values["accessible" + ItemType.Entity.ToString()] = false;

			          gs.worldState._hasWeapon = false;
			          gs.worldState.values["has" + ItemType.WeaponChest.ToString()] = gs.worldState._hasWeapon;
			          gs.worldState.values["accessible" + ItemType.WeaponChest.ToString()] = true;
			          return gs;
		          })
	          ,
	          new GoapAction("Escape")
	          .SetCost(4f)
	          .SetItem(ItemType.Escape)
	          .Pre((gs) =>
	          {
		          return gs.worldState._inSightEnemies > 0 &&
		                 gs.worldState.values["canEscape"]; 
	          })
	          .Effect((gs) => 
	          { 
		          gs.worldState._inSightEnemies = 0; 
		          gs.worldState.values["canEscape"] = false; 
		          
		          return gs; 
	          })
	          
	         , new GoapAction("PickUp") //Tesoro
	             .SetCost(3f)
	             .SetItem(ItemType.Treasure)
	             .Pre(gs =>
	             {
		             return gs.worldState._inSightEnemies == 0 &&
		                    gs.worldState.values.ContainsKey("canEscape") &&
		                    !gs.worldState.values["canEscape"] &&
		                    
		                    gs.worldState.values.ContainsKey("accessible" + ItemType.Treasure.ToString()) &&
		                    gs.worldState.values["accessible" + ItemType.Treasure.ToString()] &&
		                    
		                    !gs.worldState._treasure &&
		                    gs.worldState.values.ContainsKey("has" + ItemType.Treasure.ToString()) &&
							!gs.worldState.values["has" + ItemType.Treasure.ToString()];
	             })
	             .Effect(gs =>
	             {
		             gs.worldState._treasure = true;
		             gs.worldState.values["has" + ItemType.Treasure.ToString()] = gs.worldState._treasure;
		             gs.worldState.values["accessible" + ItemType.Treasure.ToString()] = false;
			             
		             gs.worldState._inSightEnemies = 2;
		             gs.worldState.values["canEscape"] = true;
		             
		             return gs;
	             })

	         , new GoapAction("PickUp") //Weapon
	             .SetCost(2f)
	             .SetItem(ItemType.WeaponChest)
	             .Pre(gs =>
	             {
		             return gs.worldState._inSightEnemies == 0 &&
		                    gs.worldState.values.ContainsKey("canEscape") &&
		                    !gs.worldState.values["canEscape"] &&
		                    
		                    gs.worldState.values.ContainsKey("accessible" + ItemType.WeaponChest.ToString()) &&
		                    gs.worldState.values["accessible" + ItemType.WeaponChest.ToString()] &&
		                    
		                    !gs.worldState._hasWeapon &&
		                    gs.worldState.values.ContainsKey("has" + ItemType.WeaponChest.ToString()) &&
		                    !gs.worldState.values["has" + ItemType.WeaponChest.ToString()];
	             })
	             .Effect(gs =>
	             {
		             gs.worldState._hasWeapon = true;
		             gs.worldState.values["has" + ItemType.WeaponChest.ToString()] = gs.worldState._hasWeapon;
		             gs.worldState.values["accessible" + ItemType.WeaponChest.ToString()] = false;
		             
		             gs.worldState._inSightEnemies = 1;
		             gs.worldState.values["canEscape"] = true;
		             return gs;
	             })

	         , new GoapAction("Heal")
	             .SetCost(5f)
	             .SetItem(ItemType.Heal)
	             .Pre(gs =>
	             {
		             return gs.worldState._inSightEnemies == 0 &&
		                    gs.worldState.values.ContainsKey("canEscape") &&
		                    !gs.worldState.values["canEscape"] &&
		                    
		                    gs.worldState._life < gs.worldState._maxLife &&
		                    !gs.worldState._isHealthy &&
		                    !gs.worldState.values["isHealthy"] &&
		                    
		                    gs.worldState.values.ContainsKey("accessible" + ItemType.Heal.ToString()) &&
		                    gs.worldState.values["accessible" + ItemType.Heal.ToString()];
	             })
	             .Effect(gs =>
	             {
		             gs.worldState._life = gs.worldState._maxLife;
		             gs.worldState._isHealthy = gs.worldState._life > (gs.worldState._maxLife *.5f);
		             gs.worldState.values["isHealthy"] = gs.worldState._life > (gs.worldState._maxLife *.5f);
		             
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
