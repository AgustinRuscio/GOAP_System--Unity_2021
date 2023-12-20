using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class Goap : MonoBehaviour
{
    //El satisfies y la heuristica ahora son Funciones externas
	public static IEnumerable<GoapAction> Execute(GoapState from, GoapState to, Func<GoapState, bool> satisfies, Func<GoapState, float> h, IEnumerable<GoapAction> actions)
    {
        int watchdog = 200;

        IEnumerable<GoapState> seq = AStarNormal<GoapState>.Run(
            from,
            to,
            (curr,goal)  => h (curr),
            satisfies,
            curr =>
            {
                if (watchdog == 0)
                {
                    Debug.Log("Dale flaco");
                    return Enumerable.Empty<AStarNormal<GoapState>.Arc>();
                }
                else
                    watchdog--;
                
                //en este Where se evaluan las precondiciones, al ser un diccionario de <string,bool> solo se chequea que todas las variables concuerdes
                //En caso de ser un Func<...,bool> se utilizaria ese func de cada estado para saber si cumple o no
                return actions//.Where(action => action.preconditions.All(kv => kv.In(curr.worldState.values)))
                              .Where(a => a.Preconditions(curr)) // Agregue esto para chequear las precondiuciones puestas  en el Func, Al final deberia quedar solo esta
                              .Aggregate(new FList<AStarNormal<GoapState>.Arc>(), (possibleList, action) =>
                              {
                                  Debug.Log("-----------START-------------------" );
                                  Debug.Log("Current Action " + action.Name);
                                  Debug.Log("Pre condiciones actuales " + curr);
                                  
                                  
                                  var newState = new GoapState(curr);
                                  
                                  
                                  newState = action.Effects(newState); // se aplican lso effectos del Func
                                  newState.generatingAction = action;
                                  newState.step = curr.step+1;
                                  
                                  Debug.Log("Efectos actuales " + action.Effects(newState));
                                  Debug.Log("-------------NEXT-------------" );
                                  return possibleList + new AStarNormal<GoapState>.Arc(newState, action.Cost);
                              });
            });

        if (seq == null)
        {
            Debug.Log("Imposible planear");
            return null;
        }

        foreach (var act in seq.Skip(1))
        {
			Debug.Log(act);
        }

		Debug.Log("WATCHDOG " + watchdog);
		
		return seq.Skip(1).Select(x => x.generatingAction);
	}
}
