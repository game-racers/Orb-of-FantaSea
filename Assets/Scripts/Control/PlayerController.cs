using gameracers.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace gameracers.Control
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] Image powerBG;
        Image powerBar;
        [SerializeField] float powerMod = 1f;
        
        float power;
        [SerializeField] float powerMax = 100f;
        Transform cam;

        BallMover mover;

        void Start()
        {
            cam = transform.GetChild(0).GetChild(0);
            powerBar = powerBG.transform.GetChild(0).GetComponent<Image>();
            mover = GetComponent<BallMover>();
        }

        void Update()
        {
            // First frame of mouse button being held down
            if (Input.GetMouseButtonDown(0))
            {
                power = 0;
                powerBG.gameObject.SetActive(true);
                powerBar.fillAmount = power;
            }

            // Each frame the mouse button is held down
            if (Input.GetMouseButton(0))
            {
                power += Input.GetAxis("Mouse Y") * powerMod;
                power = Mathf.Max(power, 0);
                power = Mathf.Min(power, powerMax);
                powerBar.fillAmount = power / powerMax;
            }

            // When the mouse button is released
            if (Input.GetMouseButtonUp(0))
            {
                powerBG.gameObject.SetActive(false);
                Vector3 facing = transform.position - cam.position;
                facing = new Vector3(facing.x, 0, facing.z);
                if (Mathf.Approximately(power, 0)) return;
                mover.Strike(power, facing.normalized);
            }
        }
    }
}
