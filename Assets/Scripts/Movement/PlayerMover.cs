using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace gameracers.Movement
{
    public class PlayerMover : MonoBehaviour
    {
        float gravity = -9.81f;
        [SerializeField] float airResistance = .26f;
        [SerializeField] float grassFriction = 2f;
        [SerializeField] float grassBounce = .50f;
        [SerializeField] float stoneFriction = 1f;
        [SerializeField] float stoneBounce = .90f;

        Rigidbody rb;

        Vector3 velocity = Vector3.zero;
        [SerializeField] LayerMask groundMask;
        float airTime;
        Grounded contactType = Grounded.Air;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            switch (contactType)
            {
                case Grounded.Air:
                    break;
                case Grounded.Grass:
                    velocity *= (1 - grassFriction);
                    if (velocity.magnitude < .1f)
                        velocity = Vector3.zero;
                    break;
                case Grounded.Stone:
                    velocity *= (1 - stoneFriction);
                    if (velocity.magnitude < .1f)
                        velocity = Vector3.zero;
                    break;
            }

            velocity *= (1 - airResistance);
            if (Time.time - airTime > .3f)
                velocity = new Vector3(velocity.x, velocity.y + (Time.time - airTime - .3f) * gravity, velocity.z);
            else
                velocity = new Vector3(velocity.x, 0, velocity.z);
            rb.MovePosition(transform.position + velocity * Time.fixedDeltaTime);
        }

        public void Strike(float power, Vector3 facing)
        {
            velocity = facing * power;
        }

        public void Bounce(Collider collision)
        {
            // Ground
            if ((groundMask & (1 << collision.gameObject.layer)) != 0)
            {
                airTime = Time.time;
                if (collision.gameObject.layer == LayerMask.NameToLayer("Grass"))
                {
                    contactType = Grounded.Grass;
                    Vector3 nearestPoint = collision.ClosestPoint(transform.position) - transform.position;
                    velocity = Vector3.Reflect(velocity, nearestPoint.normalized);
                    Debug.Log(velocity);
                    if (velocity.y > .1f)
                    {
                        rb.AddForce(Vector3.up * velocity.y);
                    }
                    else
                    {
                        velocity = new Vector3(velocity.x, 0f, velocity.z);
                        Debug.Log("Reset Velocity");
                    }
                }
                if (collision.gameObject.layer == LayerMask.NameToLayer("Stone"))
                {
                    contactType = Grounded.Stone;
                    Vector3 nearestPoint = transform.position - collision.ClosestPoint(transform.position);
                    velocity = Vector3.Reflect(velocity, nearestPoint) * stoneBounce;
                    if (velocity.y < 1f)
                        velocity = new Vector3(velocity.x, 0f, velocity.z);
                }
            }
        }

        private void CheckCollision()
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, .55f, Vector3.down, 0f, groundMask);
            foreach (RaycastHit hit in hits)
            {
                if ((groundMask & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grass"))
                    {
                        contactType = Grounded.Grass;
                    }
                    if (hit.collider.gameObject.layer == LayerMask.GetMask("Stone"))
                    {
                        contactType = Grounded.Stone;
                    }
                    airTime = Mathf.Infinity;
                    return;
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if ((groundMask & (1 << collision.gameObject.layer)) != 0)
            {
                airTime = Time.time;
                contactType = Grounded.Air;
                CheckCollision();
            }
        }

        
    }
}
