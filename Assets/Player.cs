using System.Collections.Generic;
using IA2; 
using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public enum Actions
{
    PickUp,
    Fight, 
    Escape,
    Heal,
    
    NextStep,
    Fail,
    Success
}

public class Player : MonoBehaviour
{
    private EventFSM<Actions> _fsm;

    private Item _target;
    
    private Entity _ent;
    
    IEnumerable<Tuple<Actions, Item>> _plan;

    public Weapon _weaponPrefab;

    public Weapon _currentWeapon;
    
    private void PickUp(Entity us, Item other)
    {
        //Aca pongo lo que tiene que pasar si aggaro el arma. Lo que pense que iab en el cofre lo ejecuto aca
        
        Debug.Log("Pick Up",other.gameObject);
        
        if(other != _target) return;
        
        _ent.AddItem(other);
        
        if (other.type == ItemType.WeaponChest)
        {
            var newWeapon = Instantiate(_weaponPrefab, transform);
            _currentWeapon = newWeapon;
            Debug.Log("Weapon",other.gameObject);
        }
        else
        {
            Debug.Log("Treasure",other.gameObject);
        }
        
        other.OnInventoryAdd();
        
        _fsm.Feed(Actions.NextStep);
    }

    private void Fight(Entity us, Item other)
    {
        if(other != _target) return;
        
        Item weapon = null;
        
        foreach (var VARIABLE in _ent.items)
        {
            if (VARIABLE.type == ItemType.Weapon)
            {
                weapon = VARIABLE;
                break;
            }
        }

        if (weapon == null)
        {
            Debug.Log("No weapon but try to fight");
            _fsm.Feed(Actions.Fail);
        }
        
        other.Kill();
        
        Destroy(_ent.Removeitem(weapon).gameObject);
        
        _fsm.Feed(Actions.NextStep);
    }

    private void Heal(Entity us, Item other)
    {
        if(other != _target) return;
        //Hacer animacion
        _fsm.Feed(Actions.NextStep);
    }
    
    [SerializeField]
    private Transform[] _hidePos;

    private Transform _escapeTarget;
    
    private void Start()
    {
       _ent = GetComponent<Entity>();

        #region FSM
        
        var any = new State<Actions>("any");

        var idle = new State<Actions>("idle");
        var bridgeStep = new State<Actions>("planStep");
        var fail = new State<Actions>("Fail");
        var success = new State<Actions>("success");
        
        var pickUp = new State<Actions>("PickUp");
        var fight = new State<Actions>("Fight");
        var escape = new State<Actions>("Hide");
        var heal = new State<Actions>("Heal");

        bridgeStep.OnEnter += a =>
        {
            var step = _plan.FirstOrDefault();
            
            if (step == null)
                _fsm.Feed(Actions.Success);
            
            _plan = _plan.Skip(1);
            var oldTarget = _target;
            _target = step.Item2;
            
            if (!_fsm.Feed(step.Item1))
                _target = oldTarget;
        };
        
        fail.OnEnter += a =>
        {
            _ent.Stop(); 
            Debug.Log("Plan failed");
        };
        
        success.OnEnter += a => Debug.Log("Success");
        

        heal.OnEnter += a =>
        {
            _ent.GoTo(_target.transform.position);
            _ent.OnHitItem += Heal;
        };

        heal.OnExit += a => _ent.OnHitItem -= Heal;
        
        
        pickUp.OnEnter += a => 
        {
            _ent.GoTo(_target.transform.position);
            _ent.OnHitItem += PickUp; //On hit item es lo que hago caundo obtengo el item
        };
        
        pickUp.OnExit += a => _ent.OnHitItem -= PickUp;

        escape.OnEnter += a =>
        {
            int rand = Random.Range(0, _hidePos.Length);

            _escapeTarget = _hidePos[rand];
            
            _ent.GoTo(_escapeTarget.position);

            if (Vector3.Distance(transform.position, _escapeTarget.position) <= 2f)
                _fsm.Feed(Actions.NextStep);
        };

        escape.OnUpdate += () =>
        {
            Debug.Log("Hiding");
            
            if (Vector3.Distance(transform.position, _escapeTarget.position) <= 2f)
                _fsm.Feed(Actions.NextStep);
        };
        
        fight.OnEnter += a =>
        {
            _ent.GoTo(_target.transform.position);
            _ent.OnHitItem += Fight;
        };
        
        fight.OnExit += a=>_ent.OnHitItem -= Fight;
        
        StateConfigurer.Create(any)
            .SetTransition(Actions.NextStep, bridgeStep)
            .Done();
        
        StateConfigurer.Create(escape)
            .SetTransition(Actions.NextStep, bridgeStep)
            .SetTransition(Actions.Fail, fail)
            .Done();
        
        StateConfigurer.Create(bridgeStep)
            .SetTransition(Actions.PickUp, pickUp)
            .SetTransition(Actions.Escape, escape)
            .SetTransition(Actions.Fight, fight)
            .SetTransition(Actions.Heal, heal)
            .SetTransition(Actions.Success, success)
            .Done();
        
        
        _fsm = new EventFSM<Actions>(idle, any);
        
        #endregion
    }

    public void ExecutePlan(List<Tuple<Actions, Item>> plan) {
        _plan = plan;
        _fsm.Feed(Actions.NextStep);
    }
    private void Update()
    {
        _fsm.Update();
    }
}