﻿using UnityEngine;
using System.Collections;

public enum ItemType
{
	Invalid,
	Key,
	Door,
	Mace,
	PastaFrola,
	//----Nuevos
	Entity,
	Treasure,
	Weapon,
	WeaponChest,
	Heal,
	Escape
}

public class Item : MonoBehaviour
{
	public ItemType type;
	private Waypoint _wp;
	private bool _insideInventory;

	public void OnInventoryAdd()
    {
		Destroy(GetComponent<Rigidbody>());
		GetComponent<Collider>().enabled = false;
		
		_insideInventory = true;
		if(_wp)
			_wp.nearbyItems.Remove(this);
	}

	public void OnInventoryRemove()
    {
		gameObject.AddComponent<Rigidbody>();
		
		GetComponent<Collider>().enabled = true;
		_insideInventory = false;
	}

	private void Start ()
    {
		_wp = Navigation.instance.NearestTo(transform.position);
		_wp.nearbyItems.Add(this);
	}

	public void Kill()
    {
		var ent = GetComponent<Entity>();
		if(ent != null)
        {
			foreach(var it in ent.RemoveAllitems())
				it.transform.parent = null;
		}
		ent._animator.SetTrigger("Death");
		Destroy(gameObject);
	}

	private void OnDestroy()
    {
		_wp.nearbyItems.Remove(this);
	}

	private void Update ()
    {
		if(!_insideInventory)
        {
			_wp.nearbyItems.Remove(this);
			_wp = Navigation.instance.NearestTo(transform.position);
			_wp.nearbyItems.Add(this);
		}
	}
}
