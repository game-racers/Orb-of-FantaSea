using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace gameracers.Movement
{
    public class PlayerMover : MonoBehaviour
    {
        float gravity = -9.81f;
        [SerializeField] float airResistance = 1f;
        [SerializeField] float allBounceStrength = .4f;
        [SerializeField] float grassFriction = .95f;
        [SerializeField] float grassBounce = .3f;
        [SerializeField] float stoneFriction = .98f;
        [SerializeField] float stoneBounce = .7f;
        [SerializeField] float ceramicFriction = .98f;
        [SerializeField] float ceramicBounce = 1f;

        Rigidbody rb;
        SphereCollider sc;

        Vector3 velocity = Vector3.zero;
        [SerializeField] LayerMask groundMask;
        float airTime;
        float lastBounceVelocity = 0f;
        float lastBounceYPos = Mathf.Infinity;
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
            if (movePoints.Count > 0)
            {
                transform.position = movePoints[movePoints.Count - 1];
            }

            movePoints.Clear();
            moveValues.Clear();

            CheckGravity();
            //CalculateRoll();
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
            if (velocity.magnitude < .05f) return;

            float remainingPower = TestCollision(velocity.magnitude * Time.fixedDeltaTime);
            int loopProtection = 0;

            while (remainingPower >= .05f && loopProtection < 10)
            {
                remainingPower = TestCollision(remainingPower);
                //loopProtection += 1;
            }
        }

        private float TestCollision(float remainingPower)
        {
            if (remainingPower < .05f) return 0;

            // referencePoint is where the ball is/will be at the time of the this TestCollision
            Vector3 referencePoint = transform.position;
            if (movePoints.Count > 0)
                referencePoint = movePoints[^1];

            float trueDist = 0; // dist from balls last position to collision point, will be appended to moveValues;
            Vector3 collisionPoint = Vector3.zero;

            Vector3 closestPoint;
            Vector3 surfaceNormal;

                // Raycast towards direction of velocity
                RaycastHit[] hits = Physics.SphereCastAll(referencePoint, sc.radius, velocity.normalized, remainingPower);

            foreach (RaycastHit hit in hits)
            {
                if (GameObject.ReferenceEquals(this.gameObject, hit.collider.gameObject))
                    continue;

                // test if raycast is behind the ball
                if (CheckSphereExtra(hit.collider, sc, out closestPoint, out surfaceNormal))
                {
                    continue;
                }

                // test raycasthit
                if ((groundMask & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    Debug.Log("Bounce");

                    Vector3 localTravel = hit.collider.ClosestPoint(referencePoint) - referencePoint;

                    if (CheckSphereExtra(hit.collider, sc, out closestPoint, out surfaceNormal))
                    {
                        localTravel = closestPoint - referencePoint;
                    }

                    collisionPoint = referencePoint + localTravel.normalized * (localTravel.magnitude - sc.radius); // world position of the ball at point of impact. 

                    float angle = Vector3.Angle(velocity, (referencePoint - closestPoint).normalized);
                    if (angle < 90 || Mathf.Approximately(angle, 90))
                    {
                        Debug.Log("Vel.Norm: " + velocity.normalized + " floor: " + (referencePoint - closestPoint).normalized + " angle: " + angle);
                        continue;
                    }
                    else
                    {
                        // limit velocity based off of last velocity and last y value of bounce
                        float tempVel = velocity.magnitude;

                        velocity = Vector3.Reflect(velocity, (referencePoint - closestPoint).normalized);

                        //TODO Calculate new velocity
                        // if current yPos is greater than lastYPos, the resulting velocity's magnitude will be less, otherwise, process it as regular
                        if (lastBounceVelocity != 0f && collisionPoint.y >= lastBounceYPos)
                        {
                            velocity = velocity.normalized * (lastBounceVelocity * allBounceStrength);
                        }

                        while (lastBounceVelocity != 0 && velocity.magnitude > lastBounceVelocity)
                        {
                            velocity = velocity * allBounceStrength;
                        }

                        lastBounceVelocity = tempVel;
                        lastBounceYPos = collisionPoint.y;
                    }
                    
                    trueDist = (collisionPoint - referencePoint).magnitude;
                    if (trueDist == 0)
                    {
                        trueDist = localTravel.magnitude;
                    }

                    // Test for ground type to calculate end velocity.
                    switch (contactType)
                    {
                        case Grounded.Grass:
                            //velocity = velocity * (1 - grassBounce + (1 - (angle - 90) / 90) * grassBounce);
                            velocity *= grassBounce;
                            remainingPower = remainingPower * grassBounce;
                            break;
                        case Grounded.Stone:
                            //velocity = velocity * (1 - stoneBounce + (1 - (angle - 90) / 90) * stoneBounce);
                            velocity *= stoneBounce;
                            remainingPower = remainingPower * stoneBounce;
                            break;
                        case Grounded.Ceramic:
                            //velocity = velocity * (1 - ceramicBounce + (1 - (angle - 90) / 90) * ceramicBounce);
                            velocity *= ceramicBounce;
                            remainingPower = remainingPower * ceramicBounce;
                            break;
                    }

                    if (velocity.y <= .3f)
                    {
                        velocity = new Vector3(velocity.x, 0f, velocity.z);
                        lastBounceVelocity = 0;
                        lastBounceYPos = Mathf.Infinity;

                        Debug.Log("stop bounce");
                    }

                    while (lastBounceVelocity != 0 && velocity.magnitude > lastBounceVelocity)
                    {
                        velocity = velocity * allBounceStrength;
                    }

                    movePoints.Add(collisionPoint);
                    moveValues.Add(trueDist);
                    return remainingPower - trueDist;
                }
            }

            // no real collisions

            movePoints.Add(referencePoint + velocity.normalized * remainingPower);
            moveValues.Add(remainingPower);
            return 0;   
        }

        private void CheckGravity()
        {
            // TODO overlap sphere to then calculate potential collision between t his ball and others. figure this shit out later, probably after making the demo level for friends. 

            // is it best to just use velocity as raycast dir? i think so...

            RaycastHit[] hits = Physics.SphereCastAll(transform.position, sc.radius, Vector3.down, -gravity * Time.fixedDeltaTime, groundMask);

            // within this radius to touch
            Vector3 collisionVector = Vector3.up * (velocity.magnitude * Time.fixedDeltaTime + sc.radius);
            if (hits.Length == 0)
            {
                CalculateGravity();
                return;
            }

            foreach (RaycastHit hit in hits)
            {
                if (GameObject.ReferenceEquals(this.gameObject, hit.collider.gameObject)) continue;

                Vector3 closestPoint;
                Vector3 surfaceNormal;
                if (CheckSphereExtra(hit.collider, sc, out closestPoint, out surfaceNormal))
                {
                    collisionVector = transform.position - closestPoint;
                    airTime = Time.time;

                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grass"))
                    {
                        contactType = Grounded.Grass;
                    }
                    else if (hit.collider.gameObject.layer == LayerMask.GetMask("Stone"))
                    {
                        contactType = Grounded.Stone;
                    }
                    else
                    {
                        contactType = Grounded.Ceramic;
                    }
                }
            }

            if (collisionVector == Vector3.up * (velocity.magnitude * Time.fixedDeltaTime + sc.radius))
            {
                CalculateGravity();
                return;
            }

            // moving too slow and friction stops the ball
            if (velocity.magnitude < .01f && Vector3.Angle(Vector3.down, collisionVector.normalized) > 178f)
            {
                Debug.Log("Should not doo this that frequently, only when stopping");
                velocity = Vector3.zero;
                return;
            }

            if (Vector3.Angle(collisionVector.normalized, Vector3.up) > 5)
            {
                velocity = velocity + collisionVector.normalized * Time.fixedDeltaTime * -gravity + Vector3.down * Time.fixedDeltaTime * -gravity;
            }

            switch (contactType)
            {
                case Grounded.Grass:
                    velocity *= grassFriction;
                    break;
                case Grounded.Stone:
                    velocity *= stoneFriction;
                    break;
                case Grounded.Ceramic:
                    velocity *= ceramicFriction;
                    break;
            }
            velocity *= airResistance;
            return;
        }

        private void CalculateGravity()
        {
            if (Time.time - airTime > 0)
            {
                velocity = velocity + (Vector3.up * gravity * (Time.time - airTime));
            }
        }

        private void CalculateRoll()
        {
            // is grounded and check for slope
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, sc.radius, Vector3.down, -gravity * Time.fixedDeltaTime);

            foreach (RaycastHit hit in hits)
            {
                if (GameObject.ReferenceEquals(hit.collider.gameObject, this.gameObject)) continue;

                Vector3 closestPoint;
                Vector3 surfaceNormal;
                if (CheckSphereExtra(hit.collider, sc, out closestPoint, out surfaceNormal))
                {
                    Vector3 groundNormal = (transform.position - closestPoint).normalized * -gravity * Time.fixedDeltaTime;

                    if (Vector3.Angle(groundNormal.normalized, Vector3.up) > 5f)
                    {
                        velocity += groundNormal + Vector3.up * gravity;
                    }
                }
            }
        }

        public Vector3 GetVelocity()
        {
            return velocity;
        }

        public void ResetVelocity()
        {
            velocity = Vector3.zero;
        }

        public static bool CheckSphereExtra(Collider target_collider, SphereCollider sphere_collider, out Vector3 closest_point, out Vector3 surface_normal)
        {
            closest_point = Vector3.zero;
            surface_normal = Vector3.zero;
            float surface_penetration_depth = 0.2f;

            Vector3 sphere_pos = sphere_collider.transform.position;
            if (Physics.ComputePenetration(target_collider, target_collider.transform.position, target_collider.transform.rotation, sphere_collider, sphere_pos, Quaternion.identity, out surface_normal, out surface_penetration_depth))
            {
                closest_point = sphere_pos + (surface_normal * (sphere_collider.radius - surface_penetration_depth));

                surface_normal = -surface_normal;
                return true;
            }

            return false;
        }
    }

    //public enum Grounded
    //{
    //    Air,
    //    Grass,
    //    Stone,
    //    Ceramic
    //}
}
