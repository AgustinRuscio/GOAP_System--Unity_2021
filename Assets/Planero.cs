using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine.Serialization;

public class Planero : MonoBehaviour 
{
	private readonly List<Tuple<Vector3, Vector3>> _debugRayList = new List<Tuple<Vector3, Vector3>>();


	[Header("Initial State Variables")]
	
	[SerializeField]
	private int _initialInSightEnemies = 0;

	[SerializeField] 
	private float _initialMaxLife = 100, _initialLife = 100;

	[SerializeField]
	private bool _initialIsHealthy = true, 
		_initialTreasure = false, 
		_initialHasWeapon = false, 
		_initialCanEscape = false;

	[Header("Goal State Variables")]
	[SerializeField]
	private int _goalInSightEnemies = 0;

	[SerializeField] 
	private float _goalMaxLife = 100, _goalLife = 100;

	[SerializeField]
	private bool _goalIsHealthy = true, 
		_goalTreasure = true, 
		_goalHasWeapon = true, 
		_goalCanEscape = false;


	private IEnumerable<Item> everything;
	
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
		everything = nav.AllItems().Union(nav.AllInventories());// .Union() une 2 colecciones sin agregar duplicados(eso incluye duplicados en la misma coleccion)

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
	        _inSightEnemies = this._initialInSightEnemies,
	        
	        _maxLife = this._initialMaxLife,
	        _life = this._initialLife,
	        _isHealthy = this._initialIsHealthy,
	        
	        _treasure = this._initialTreasure,
	        _hasWeapon = this._initialHasWeapon,
	        
	        values = new Dictionary<string, bool>()
        };

        initial.worldState.values = observedState; //le asigno los valores actuales, conseguidos antes
		initial.worldState.values["canEscape"] = this._initialCanEscape;
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
			_inSightEnemies = this._goalInSightEnemies,
			
			_isHealthy = this._goalIsHealthy,
			_life = this._goalLife,
			_maxLife = this._goalMaxLife,
			
			_treasure = this._goalTreasure,
			_hasWeapon = this._goalHasWeapon,
			
			values = new Dictionary<string, bool>()
        };
        
        goal.worldState.values["has" + ItemType.Treasure.ToString()] = goal.worldState._treasure;
        goal.worldState.values["has" + ItemType.WeaponChest.ToString()] = goal.worldState._hasWeapon;
        goal.worldState.values["isHealthy"] = goal.worldState._life > (goal.worldState._maxLife *.5f);
        goal.worldState.values["canEscape"] = this._goalCanEscape;
        
        
        //goal.values["has" + ItemType.Key.ToString()] = true;
        //goal.worldState.values["has" + ItemType.PastaFrola.ToString()] = true;
        //goal.values["has"+ ItemType.Mace.ToString()] = true;
        //goal.values["dead" + ItemType.Entity.ToString()] = true;}


        Func<GoapState, float> heuristc = (curr) =>
        {
            int count = 0;

            if(curr.worldState.values["canEscape"] != this._goalCanEscape)
	            count++;
           if(curr.worldState.values["isHealthy"] != _goalIsHealthy)
           		count++;
           if (curr.worldState.values["has" + ItemType.Treasure.ToString()] != _goalTreasure)
	           count++;
           if (curr.worldState.values["has" + ItemType.WeaponChest.ToString()] != _goalHasWeapon)
	           count++;
            
            return count;
        };

        Func<GoapState, bool> objectice = (curr) =>
        {
	        return curr.worldState.values["canEscape"] == this._goalCanEscape &&
	               curr.worldState.values["isHealthy"] == this._goalIsHealthy &&
	               curr.worldState.values["has" + ItemType.WeaponChest.ToString()] == this._goalHasWeapon &&
	               curr.worldState.values["has" + ItemType.Treasure.ToString()] == this._goalTreasure;
        };
        
        var actDict = new Dictionary<string, Actions>() {
			  { "Escape", Actions.Escape }
			, { "PickUpWeapon", Actions.PickUp }
			, { "PickUpTreasure", Actions.PickUp }
			, { "Fight"	, Actions.Fight }
			, { "Heal"	, Actions.Heal }
		};
        
		var plan = Goap.Execute(initial,goal, objectice, heuristc, actions, everything);

		
		
		if(plan == null)
			Debug.Log("Couldn't plan");
		else {
			
		//	Debug.Log("Primra accion" + plan.First().Name + plan.First().item  + plan.First().Cost+ " -------------------------------------------------------------------------------------");
		//	Debug.Log("Primra accion" + plan.Skip(2).First().Name + plan.Skip(2).First().item  + plan.Skip(2).First().Cost+ " -------------------------------------------------------------------------------------");
			
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

			
			
			int cervecera = 0;
			foreach (var VARIABLE in plan)
			{
				cervecera++;
				Debug.Log(cervecera + VARIABLE.Name + VARIABLE.item);
			}
		}
	}
    
    private List<GoapAction> CreatePossibleActionsList()
    {
	     return new List<GoapAction>()
	     {
	          new GoapAction("Fight")
		          .SetItem(ItemType.Entity)
		          .SetCost(5f, transform.position, everything.FirstOrDefault(i => i.type == ItemType.Entity).transform.position)
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
	          .SetItem(ItemType.Escape)
	          .SetCost(4f, transform.position, everything.FirstOrDefault(i => i.type == ItemType.Escape).transform.position)
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
	          
	         
	         , new GoapAction("PickUpWeapon") //Weapon
	             .SetItem(ItemType.WeaponChest)
	             .SetCost(2f, transform.position, everything.FirstOrDefault(i => i.type == ItemType.WeaponChest).transform.position)
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
	         , new GoapAction("PickUpTreasure") //Tesoro
		         .SetItem(ItemType.Treasure)
		         .SetCost(3f, transform.position, everything.FirstOrDefault(i => i.type == ItemType.Treasure).transform.position)
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

	         , new GoapAction("Heal")
	             .SetItem(ItemType.Heal)
	             .SetCost(5f, transform.position, everything.FirstOrDefault(i => i.type == ItemType.Heal).transform.position)
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
