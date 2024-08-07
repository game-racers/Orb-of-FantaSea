using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

namespace gameracers.Movement
{
    public class BallMover : MonoBehaviour
    {
        float gravity = -9.81f;
        [SerializeField] float airResistance = .26f;
        [SerializeField] float grassFriction = 2f;
        [SerializeField] float grassBounce = .50f;
        [SerializeField] float stoneFriction = 1f;
        [SerializeField] float stoneBounce = .90f;
        [SerializeField] float ceramicFriction = .5f;
        [SerializeField] float ceramicBounce = 1f;

        Rigidbody rb;
        SphereCollider sc;

        Vector3 velocity = Vector3.zero;
        [SerializeField] LayerMask groundMask;
        float airTime;
        Grounded contactType = Grounded.Air;

        List<Vector3> movePoints = new List<Vector3>();
        List<float> moveValues = new List<float>();

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            sc = GetComponent<SphereCollider>();
        }

        void FixedUpdate()
        {
            // ensure ball is where it is supposed to be
            if (movePoints.Count > 0)
            {
                transform.position = movePoints[movePoints.Count - 1];
                //rb.Move(movePoints[movePoints.Count - 1], Quaternion.identity);
            }

            movePoints.Clear();
            moveValues.Clear();
            switch (contactType)
            {
                case Grounded.Air:
                    break;
                case Grounded.Ceramic:
                    break;
                case Grounded.Grass:
                    velocity *= (100 - grassFriction) / 100f;
                    break;
                case Grounded.Stone:
                    velocity *= (100 - stoneFriction) / 100f;
                    break;
            }
            velocity *= (100 - airResistance) / 100f;

            if (velocity.magnitude < .0001f)
                velocity = Vector3.zero;

            CheckCollision();
        }

        private void Update()
        {
            
        }

        public void Strike(float power, Vector3 facing)
        {
            velocity = facing * power;
        }

        private void CheckCollision()

        {
            if (velocity.magnitude < .0001f) 
                return;

            float remainingPower = TestCollision(velocity.magnitude * Time.fixedDeltaTime);

            
            while (remainingPower > 0)
            {
                remainingPower = TestCollision(remainingPower);
            }
        }

        private float TestCollision(float remainingPower)
        {
            Vector3 referencePoint = transform.position;
            if (movePoints.Count != 0)
                referencePoint = movePoints[movePoints.Count - 1];

            Vector3 endVelocity = velocity; // final velocity
            float trueDist = 0; // distance from balls last position to collision point, will be appended to a list
            float collisionDist = Mathf.Infinity; 
            Vector3 collisionPoint = Vector3.zero;

            RaycastHit[] hits = Physics.SphereCastAll(referencePoint, sc.radius, velocity.normalized, remainingPower);
            foreach (RaycastHit hit in hits)
            {
                if (GameObject.ReferenceEquals(gameObject, hit.collider.gameObject)) continue;
                if ((hit.point - transform.position).magnitude - sc.radius > collisionDist) continue;

                // raycast hit against other object. 
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
                    if (hit.collider.gameObject.layer == LayerMask.GetMask("Ceramic"))
                    {
                        contactType = Grounded.Ceramic;
                    }
                    if (Vector3.Angle(velocity, hit.point - transform.position) > 90f)
                        continue;
                    Debug.Log("BOUNCE");
                    // Calculation of the collision point, and adding the collision point to the list of locations to travel to. 
                    Vector3 offset = hit.point - transform.position;
                    collisionPoint = transform.position + velocity.normalized * (offset.magnitude - sc.radius);
                    velocity = Vector3.Reflect(velocity, (collisionPoint - hit.point).normalized);

                    endVelocity = velocity;
                    collisionDist = (collisionPoint - transform.position).magnitude;
                    trueDist = collisionDist;
                }
            }
            velocity = endVelocity;

            if (collisionPoint == Vector3.zero)
            {
                if (movePoints.Count == 0)
                {
                    movePoints.Add(transform.position + velocity * Time.fixedDeltaTime);
                    moveValues.Add(velocity.magnitude * Time.fixedDeltaTime);
                    return 0;
                }
                else
                {
                    movePoints.Add(movePoints[movePoints.Count - 1] + velocity.normalized * remainingPower);
                    moveValues.Add(remainingPower);
                    return 0;
                }
            }
            else 
            {
                movePoints.Add(collisionPoint);
                moveValues.Add(trueDist);
            }
            return remainingPower - trueDist;
        }
    }

    public enum Grounded
    {
        Air,
        Grass,
        Stone,
        Ceramic
    }
}
