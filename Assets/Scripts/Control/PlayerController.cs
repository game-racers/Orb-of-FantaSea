using gameracers.Movement;
using gameracers.Clothing;
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
        Transform facing;
        Vector3 lastDir;

        PlayerMover mover;
        List<Equipment> equipment = new List<Equipment>();

        bool canSwing = false;

        int swings;

        void Start()
        {
            cam = transform.GetChild(0).GetChild(0);
            powerBar = powerBG.transform.GetChild(0).GetComponent<Image>();
            mover = GetComponent<PlayerMover>();
            facing = transform.Find("Facing");

            foreach (Transform child in transform)
            {
                if (child.GetComponent<Equipment>() != null)
                    equipment.Add(child.GetComponent<Equipment>());
            }
        }

        void Update()
        {
            // First frame of mouse button being held down
            if (Input.GetMouseButtonDown(0))
            {
                if (mover.GetVelocity().magnitude > .3f)
                {
                    canSwing = false;
                    return;
                }
                canSwing = true;
                power = 0;
                powerBG.gameObject.SetActive(true);
                powerBar.fillAmount = power;
            }

            // Each frame the mouse button is held down
            if (Input.GetMouseButton(0))
            {
                if (!canSwing) return;
                power += Input.GetAxis("Mouse Y") * powerMod;
                power = Mathf.Max(power, 0);
                power = Mathf.Min(power, powerMax);
                powerBar.fillAmount = power / powerMax;
            }

            // When the mouse button is released
            if (Input.GetMouseButtonUp(0))
            {
                if (!canSwing) return;
                powerBG.gameObject.SetActive(false);
                Vector3 facing = transform.position - cam.position;
                facing = new Vector3(facing.x, 0, facing.z);
                if (Mathf.Approximately(power, 0)) return;
                mover.Strike(power, facing.normalized);
                canSwing = false;
                swings += 1;
            }
        }

        private void LateUpdate()
        {
            Vector3 temp = mover.GetVelocity();
            if (temp.magnitude < .5f)
            {
                facing.localPosition = lastDir;
                return;
            }

            facing.localPosition = Vector3.Lerp(facing.localPosition, temp.normalized, .03f);
            if (temp.magnitude > 1f)
                lastDir = temp.normalized;
            foreach (Equipment item in equipment)
            {
                item.AimFace(facing.localPosition);
            }
        }

        public int GetSwings()
        {
            return swings;
        }

        public void AddSwing()
        {
            swings += 1;
        }

        public void ResetSwings()
        {
            swings = 0;
        }

        public void ResetVelocity()
        {
            mover.ResetVelocity();
        }
    }
}
