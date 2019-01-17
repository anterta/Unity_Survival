using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class ThirdPersonUserControl : MonoBehaviour
    {
        private ThirdPersonCharacter m_Character; // A reference to the ThirdPersonCharacter on the object
        private Transform m_Cam;                  // A reference to the main camera in the scenes transform
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool m_Jump, m_crouch;                      // the world-relative desired move direction, calculated from the camForward and user input.
        private float m_h,m_v;        
	    private PlayerPickUp _pickUp;

        
        private void Start()
        {
            // get the transform of the main camera
            if (Camera.main != null)
            {
                m_Cam = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning(
                    "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
                // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
            }

            // get the third person character ( this should never be null due to require component )
            m_Character = GetComponent<ThirdPersonCharacter>();
		    _pickUp = GetComponent<PlayerPickUp>();
        }


        private void Update()
        {            
            if(!_pickUp.IsActif()) {
                if (!m_Jump)
                {
                    m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
                }
                
                // read inputs
                m_h = CrossPlatformInputManager.GetAxis("Horizontal");
                m_v = CrossPlatformInputManager.GetAxis("Vertical");
                m_crouch = Input.GetKey(KeyCode.C);
            }
        }


        // Fixed update is called in sync with physics
        private void FixedUpdate()
        {
            // calculate move direction to pass to character
            if (m_Cam != null)
            {
                // calculate camera relative direction to move:
                m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = m_v*m_CamForward + m_h*m_Cam.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = m_v*Vector3.forward + m_h*Vector3.right;
            }
#if !MOBILE_INPUT
			// walk speed multiplier
	        if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

            // pass all parameters to the character control script
            m_Character.Move(m_Move, m_crouch, m_Jump, _pickUp.IsPickUp());
            m_Jump = false;
        }

        
        public bool GoTo(GameObject to) {
            bool goodPlace = false;
            Vector3 targetDir = to.transform.position - transform.position;

            float angle = Vector3.SignedAngle(targetDir, transform.forward, Vector3.up);
            if(angle > 25f) {
                m_h = Mathf.Clamp(m_h - .3f * Time.deltaTime, -1f, 1f);
            } else if(angle < -25f) {
                m_h = Mathf.Clamp(m_h + .3f * Time.deltaTime, -1f, 1f);
            } else {
                if(m_h > 0) {
                    m_h = m_h - .6f * Time.deltaTime;
                } else if(m_h < 0) {
                    m_h = m_h + .6f * Time.deltaTime;
                } 
                
                float sqrMagnitude = Vector3.SqrMagnitude(targetDir);
                if(sqrMagnitude > 1f) {
                    m_v = Mathf.Clamp(m_v + .3f * Time.deltaTime, 0, .5f);
                } else {
                    m_v = 0;
                    m_h = 0;
                    goodPlace = true;
                }
            }

            return goodPlace;
        }
    }
}
