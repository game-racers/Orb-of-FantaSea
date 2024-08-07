using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using gameracers.Movement;

namespace gameracers.Control
{
    public class SphereCollider : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            transform.parent.GetComponent<PlayerMover>().Bounce(other);
        }
    }
}
