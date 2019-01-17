using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickUp : MonoBehaviour {

	private Animator anim;
	private bool _goPickUp = false;
	private bool _inHand = false;
	private GameObject _itemToPick = null;
    private Inventory _inventory;
	private GameObject _rightHand;
	private UnityStandardAssets.Characters.ThirdPerson.ThirdPersonUserControl _aniControl;

	void Start () {
		anim = GetComponent<Animator>();
		_aniControl = GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonUserControl>();
		_inventory = GetComponent<PlayerInventory>().inventory.GetComponent<Inventory>();
		_rightHand =  GameObject.Find("rHand");
	}
	
	// Update is called once per frame
	void Update () {
		if (_itemToPick == null  && Input.GetKeyDown(KeyCode.E)) {
				findItem();
		} else if(_goPickUp) {
			_goPickUp = !_aniControl.GoTo(_itemToPick);
			if(!_goPickUp)
				anim.SetTrigger("pickUp");
		}

		if(_itemToPick != null && !_goPickUp) {
			if(_inHand && anim.GetFloat("itemInHand") > .7f) {
				PickUp();
			} else if(!_inHand && anim.GetFloat("itemInHand") > .2f) {
				_inHand = true;
				_itemToPick.transform.parent =_rightHand.transform;
				_itemToPick.GetComponent<Rigidbody>().isKinematic = true;
				Item item = _inventory.getItemByID(_itemToPick.GetComponent<PickUpItem>().item.itemID);
				_itemToPick.transform.localPosition = item.positionHandle;
				_itemToPick.transform.localEulerAngles = item.rotationHandle;
			}
		}
	}

	
	private void OnAnimatorIK(int layerIndex) {
		if(_itemToPick != null && !_goPickUp && !_inHand) {
			anim.SetIKPositionWeight(AvatarIKGoal.RightHand, anim.GetFloat("rightHand"));
			anim.SetIKPosition(AvatarIKGoal.RightHand, _itemToPick.transform.position);
		}
	}

	public bool IsActif() { return _itemToPick != null;}

	public bool IsPickUp() { return (_itemToPick != null && !_goPickUp); }

	
	private void findItem() {
		GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
		for(int i=0; i<items.Length; i++) {
            float distance = Vector3.Distance(items[i].transform.position, transform.position);
			Item item = _inventory.getItemByID(items[i].GetComponent<PickUpItem>().item.itemID);

            if (distance <= item.distancePickUp)
            {
				_itemToPick = items[i];
				_goPickUp = !_aniControl.GoTo(_itemToPick);
				return;
			}
		}
	}
	
	private void PickUp() {
		_inHand = false;
		Item item = _itemToPick.GetComponent<PickUpItem>().item;
		bool check = _inventory.checkIfItemAllreadyExist(item.itemID, item.itemValue);
		if (check) {
			Destroy(_itemToPick);
		} else if (_inventory.ItemsInInventory.Count < (_inventory.width * _inventory.height))
		{
			_inventory.addItemToInventory(item.itemID, item.itemValue);
			_inventory.updateItemList();
			_inventory.stackableSettings();
			Destroy(_itemToPick);
		} else {
			_itemToPick.transform.parent = null;
			_itemToPick.GetComponent<Rigidbody>().isKinematic = false;
		}
		_itemToPick = null;
	}

}
