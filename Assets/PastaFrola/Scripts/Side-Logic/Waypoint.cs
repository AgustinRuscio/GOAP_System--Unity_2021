using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Waypoint : MonoBehaviour
{
	public List<Waypoint> adyacent;
	public HashSet<Item> nearbyItems = new HashSet<Item>();

	[SerializeField]
	private LayerMask _nodeLayer, _obstacles;

	[SerializeField]
	private float _radius;
	
	[SerializeField]
	private bool _needsToDraw;

	void Start ()
	{
		adyacent = Physics.OverlapSphere(transform.position, _radius, _nodeLayer)
			.Select(x => x.GetComponent<Waypoint>())
			.Where(x => x != null && x != this && Tools.InLineOfSight(transform.position, x.transform.position, _obstacles))
			.ToList();
		//Make bidirectional
		//foreach(var wp in adyacent) {
		//	if(wp != null && wp.adyacent != null) {
		//		if(!wp.adyacent.Contains(this))
		//			wp.adyacent.Add(this);
		//	}
		//}
		//adyacent = adyacent.Where(x=>x!=null).Distinct().ToList();
	}
	
	void Update ()
	{
		//For debugging: Pause then inactivate
		nearbyItems.RemoveWhere(it => !it.isActiveAndEnabled);
	}
	
	void OnDrawGizmos()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(transform.position, 0.3f);
	    
		if(!_needsToDraw) return;
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(transform.position, _radius);
		
		//Gizmos.color = Color.blue;
		//foreach(var wp in adyacent)
		//{
		//    Gizmos.DrawLine(transform.position, wp.transform.position);
		//}
	}
}
