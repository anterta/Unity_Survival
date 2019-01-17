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
	private Vector3 animStep;
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

	private PlayerPickUp _pickUp;

	void Start () {
		anim = GetComponent<Animator>();
		vSpeed = 0;
		hSpeed = 0;
		_pickUp = GetComponent<PlayerPickUp>();
	}

	void Update () {
		if(!_pickUp.IsActif())
			AnimController();
	}
	/*
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
		}/*else if(Input.GetKey(inputBack)) {
			vSpeed = Mathf.Clamp(vSpeed - vAcceleration * Time.deltaTime, -1, 1);
		}*/ else {
			vSpeed = Mathf.Clamp(vSpeed - 2*vAcceleration * Time.deltaTime, 0, 1);
		}
		animStep = Vector3.forward * walkStepDistance*vSpeed*Time.deltaTime;
		
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

		if(Input.GetKey(inputJump))
			anim.SetTrigger("jump");

		anim.SetFloat("vSpeed", vSpeed);
		anim.SetFloat("hSpeed", hSpeed);
	}

	public bool GoTo(GameObject to) {
		bool goodPlace = false;
        Vector3 targetDir = to.transform.position - transform.position;

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
			} else { 
				vSpeed = Mathf.Clamp(vSpeed - 2*vAcceleration * Time.deltaTime, 0, 1); 

				goodPlace = true;
			}
			animStep = Vector3.forward * walkStepDistance*vSpeed*Time.deltaTime;
		}
		
		anim.SetFloat("vSpeed", vSpeed);
		anim.SetFloat("hSpeed", hSpeed);

		return goodPlace;
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
		anim.bodyPosition += animStep;

		if(!enableFeetIK) return;
		MovePelvisHeight();

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

}
