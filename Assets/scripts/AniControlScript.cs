using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AniControlScript : MonoBehaviour {

	#region AnimVariables
	private Animator anim;
	public string inputFront;
	public string inputBack;	
	public string inputLeft;
	public string inputRight;
	public string inputJump;
	[Range(0,2)] [SerializeField] public float vAcceleration;
	[Range(0,2)] [SerializeField] public float hAcceleration;
	[Range(0,10)] [SerializeField] public float walkStepDistance;
	[Range(0,180)] [SerializeField] public float turnStepAngle;
	private float vSpeed;
	private float hSpeed;
	#endregion
	
	#region IkVariables
	private bool isGrounded;

	private Vector3 rightFootPosition, leftFootPosition, rightFootIKPosition, leftFootIKPosition;
	private Quaternion rightFootIKRotation, leftFootIKRotation;
	private float lastPelvisPositionY, lastRightFootPositionY, lastLeftFootPositionY;

	public bool enableFeetIK = true;
	[Range(0,2)] [SerializeField] private float heightFromGroundRaycast = 1.14f;
	[Range(0,2)] [SerializeField] private float raycastDownDistance = 1.14f;
	[Range(0,1)] [SerializeField] private float pelvisUpAndDownSpeed = .28f;
	[Range(0,1)] [SerializeField] private float feetToIkPositionSpeed = .5f;
	[SerializeField] private float pelvisOffset = 0f;
	[SerializeField] private LayerMask environnementLayer;
	#endregion

	#region PickUpVariables
	private GameObject _itemToPick = null;
    private Inventory _inventory;
	private GameObject _rightHand;
	#endregion

	void Start () {
		anim = GetComponent<Animator>();
		vSpeed = 0;
		hSpeed = 0;
		_inventory = GetComponent<PlayerInventory>().inventory.GetComponent<Inventory>();
		_rightHand =  GameObject.Find("rHand");
	}

	void Update () {
		if(_itemToPick != null)
			goPickUp();
		else {
			if (_inventory != null && Input.GetKeyDown(KeyCode.E))
				findItem();
		}
	}/*
	public float speed;
	public float rotateHead;
	public float rotationSpeed;
	public float minCamRotation;
	public float maxCamRotation;

	void Update()
	{
		rotateHead = Input.mousePosition.x/Screen.width - .5f;
		transform.Rotate(Vector3.up, rotateHead * rotationSpeed);

		Transform cam = gameObject.transform.Find("Main Camera");
		float camRotate = Mathf.Clamp(Input.mousePosition.y/Screen.height - .5f, minCamRotation, maxCamRotation);
		Debug.Log(camRotate);
        cam.rotation = Quaternion.identity;
		cam.RotateAround(transform.position, transform.right, camRotate);
        //cam.Rotate(Vector3.left, camRotate*10);

        if (Input.GetMouseButton(0))
			gameObject.GetComponent<CharacterController>().Move(transform.TransformDirection(Vector3.forward * speed * Time.deltaTime));

	}*/

	#region AnimFunctions
	
	private void AnimController() {
		if(Input.GetKey(inputFront)) {
			vSpeed = Mathf.Clamp(vSpeed + vAcceleration * Time.deltaTime, 0, 1);
			anim.bodyPosition = anim.bodyPosition + Vector3.forward * walkStepDistance*vSpeed*Time.deltaTime;
			//transform.Translate(0,0,walkStepDistance*vSpeed*Time.deltaTime);
		}/*else if(Input.GetKey(inputBack)) {
			vSpeed = Mathf.Clamp(vSpeed - vAcceleration * Time.deltaTime, -1, 1);
			transform.Translate(0,0,-walkStepDistance*vSpeed*Time.deltaTime);
		}*/ else {
			vSpeed = Mathf.Clamp(vSpeed - 2*vAcceleration * Time.deltaTime, 0, 1);
		}
		
		if(Input.GetKey(inputLeft)) {
			hSpeed = Mathf.Clamp(hSpeed - hAcceleration * Time.deltaTime, -1f, 1f);
			transform.Rotate(Vector3.up, hSpeed * turnStepAngle * Time.deltaTime);
		} else if(Input.GetKey(inputRight)) {
			hSpeed = Mathf.Clamp(hSpeed + hAcceleration * Time.deltaTime, -1f, 1f);
			transform.Rotate(Vector3.up, hSpeed * turnStepAngle * Time.deltaTime);
		} else if(hSpeed > 0) {
			hSpeed = hSpeed - 2*hAcceleration * Time.deltaTime;
		} else if(hSpeed < 0) {
			hSpeed = hSpeed + 2*hAcceleration * Time.deltaTime;
		}  

		//myAnimator.SetBool("isJump", Input.GetKey(KeyCode.Space));
		if(Input.GetKey(inputJump))
			anim.SetTrigger("jump");

		anim.SetFloat("vSpeed", vSpeed);
		anim.SetFloat("hSpeed", hSpeed);
	}

	#endregion

	#region IkFunctions
	#region FeetGrounding

	private void FixedUpdate() {
		if(!enableFeetIK) return;
		if(anim == null) return;

		AdjustFeetTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
		AdjustFeetTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

		FeetPositionSolver(rightFootPosition, ref rightFootIKPosition, ref rightFootIKRotation);
		FeetPositionSolver(leftFootPosition, ref leftFootIKPosition, ref leftFootIKRotation);
	}

	private void OnAnimatorIK(int layerIndex) {
		if(anim == null) return;

		if(_itemToPick == null) {
			AnimController();
			if(!enableFeetIK) return;
			MovePelvisHeight();
		} else {
			if(anim.GetFloat("itemInHand") > .7f) {
				PickUp();
			} else if(anim.GetFloat("itemInHand") > .2f) {
				_itemToPick.GetComponent<BoxCollider>().isTrigger = true;
				//_itemToPick.transform.position = transform.TransformPoint( anim.GetBoneTransform(HumanBodyBones.RightIndexDistal).position);
				Item item = _inventory.getItemByID(_itemToPick.GetComponent<PickUpItem>().item.itemID);
				_itemToPick.transform.localPosition = item.positionHandle;
				_itemToPick.transform.localEulerAngles = item.rotationHandle;
				_itemToPick.transform.SetParent(_rightHand.transform);
			} else {
				anim.SetIKPositionWeight(AvatarIKGoal.RightHand, anim.GetFloat("rightHand"));
				anim.SetIKPosition(AvatarIKGoal.RightHand, _itemToPick.transform.position);
			}
		}

		if(!enableFeetIK) return;

		anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
		anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, anim.GetFloat("rightFoot"));
		
		MoveFeetToIkPoint(AvatarIKGoal.RightFoot, rightFootIKPosition, rightFootIKRotation, ref lastRightFootPositionY);
		
		anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
		anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, anim.GetFloat("leftFoot"));
		
		MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, leftFootIKPosition, leftFootIKRotation, ref lastLeftFootPositionY);
	}

	#endregion

	#region FeetGroundingMethods

	void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 positionIkHolder, Quaternion rotationIkHolder, ref float lastFootPositionY) {
		Vector3 targetIkPosition = anim.GetIKPosition(foot);

		if(positionIkHolder != Vector3.zero) {
			targetIkPosition = transform.InverseTransformPoint(targetIkPosition);
			positionIkHolder = transform.InverseTransformPoint(positionIkHolder);
		
			float yVariable = Mathf.Lerp(lastFootPositionY, positionIkHolder.y, feetToIkPositionSpeed);
			targetIkPosition.y += yVariable;

			lastFootPositionY = yVariable;
			targetIkPosition = transform.TransformPoint(targetIkPosition);

			anim.SetIKRotation(foot, rotationIkHolder);
		}

		anim.SetIKPosition(foot, targetIkPosition);
	}

	private void MovePelvisHeight() {
		if(rightFootIKPosition == Vector3.zero || leftFootIKPosition == Vector3.zero || lastPelvisPositionY == 0) {
			lastPelvisPositionY = anim.bodyPosition.y;
			return;
		}

		float lOffsetPosition = leftFootIKPosition.y - transform.position.y;
		float rOffsetPosition = rightFootIKPosition.y - transform.position.y;

		float totalOffset = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;
		totalOffset = Mathf.Lerp(lastPelvisPositionY - anim.bodyPosition.y, totalOffset, pelvisUpAndDownSpeed);

		anim.bodyPosition = anim.bodyPosition + Vector3.up * totalOffset;

		lastPelvisPositionY = anim.bodyPosition.y;
	}

	private void FeetPositionSolver(Vector3 fromSkyPosition, ref Vector3 feetIkPostions, ref Quaternion feetIkRotations) {
		RaycastHit feetOutHit;

		if(Physics.Raycast(fromSkyPosition, Vector3.down, out feetOutHit, raycastDownDistance + heightFromGroundRaycast, environnementLayer)) {
			feetIkPostions = fromSkyPosition;
			feetIkPostions.y = feetOutHit.point.y + pelvisOffset;
			feetIkRotations = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation;

			return;
		}

		feetIkPostions = Vector3.zero;
	}

	private void AdjustFeetTarget(ref Vector3 feetPositions, HumanBodyBones foot) {
		feetPositions = anim.GetBoneTransform(foot).position;
		feetPositions.y = transform.position.y + heightFromGroundRaycast;
	}

	#endregion
	#endregion

	#region PickUpFunctions

	private void findItem() {
		GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
		for(int i=0; i<items.Length; i++) {
            float distance = Vector3.Distance(items[i].transform.position, transform.position);
			Item item = _inventory.getItemByID(items[i].GetComponent<PickUpItem>().item.itemID);

            if (distance <= item.distancePickUp)
            {
				_itemToPick = items[i];
				goPickUp();
				return;
			}
		}
	}

	private void goPickUp() {
        Vector3 targetDir = _itemToPick.transform.position - transform.position;

        float angle = Vector3.SignedAngle(targetDir, transform.forward, Vector3.up);
		if(angle > 25f) {
			hSpeed = Mathf.Clamp(hSpeed - hAcceleration * Time.deltaTime, -1f, 1f);
			transform.Rotate(Vector3.up, hSpeed * turnStepAngle * Time.deltaTime);
		} else if(angle < -25f) {
			hSpeed = Mathf.Clamp(hSpeed + hAcceleration * Time.deltaTime, -1f, 1f);
			transform.Rotate(Vector3.up, hSpeed * turnStepAngle * Time.deltaTime);
		} else {
			if(hSpeed > 0) {
				hSpeed = hSpeed - 2*hAcceleration * Time.deltaTime;
			} else if(hSpeed < 0) {
				hSpeed = hSpeed + 2*hAcceleration * Time.deltaTime;
			} 
			
			float sqrMagnitude = Vector3.SqrMagnitude(targetDir);
			if(sqrMagnitude > 1f) {
				vSpeed = Mathf.Clamp(vSpeed + vAcceleration * Time.deltaTime, 0, .5f);
				//anim.bodyPosition = anim.bodyPosition + Vector3.forward * walkStepDistance*vSpeed*Time.deltaTime;
				transform.Translate(0,0,walkStepDistance*vSpeed*Time.deltaTime);
			} else {
				anim.SetTrigger("pickUp");
			}
		}
		
		anim.SetFloat("vSpeed", vSpeed);
		anim.SetFloat("hSpeed", hSpeed);
	}

	private void PickUp() {
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
			_itemToPick.GetComponent<BoxCollider>().isTrigger = false;
		}
		_itemToPick = null;
	}

	#endregion

}
