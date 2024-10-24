using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace gameracers.Control
{
    public class MiniGolfHole : MonoBehaviour
    {
        [SerializeField] string holeID;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Player")
            {
                // enable a different camera
            }
            else
            {
                if (collision.gameObject.GetComponent<Rigidbody>() != null) return;
                Destroy(collision.gameObject);
            }

            EventListener.BallHasSunk(collision.gameObject, holeID);
        }

    }
}
