using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace gameracers.Movement
{
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] float speed = .3f;

        float gravity = -9.81f;
        [SerializeField] float grassFriction = .85f;
        [SerializeField] float grassBounce = .6f;
        [SerializeField] float stoneFriction = .90f;
        [SerializeField] float stoneBounce = .8f;
        [SerializeField] float ceramicFriction = .95f;
        [SerializeField] float ceramicBounce = .8f;
        [SerializeField] float allBounceStrength = .8f;
        [SerializeField] float rollMult = 1.05f;

        [SerializeField] float bounceMin = .5f;

        [SerializeField] Grounded contactType = Grounded.Air;
        bool hasCalculatedTouch = false;

        //float downVel = 0;
        float airTime;
        float lastBounceVelocity = 0f;
        float lastBounceYPos = Mathf.Infinity;

        Rigidbody rb;
        SphereCollider sc;

        Vector3 velocity;

        [SerializeField] LayerMask groundMask;

        List<Vector3> movePoints = new List<Vector3>();

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            sc = GetComponent<SphereCollider>();
        }

        private void FixedUpdate()
        {
            if (movePoints.Count > 0)
            {
                transform.position = movePoints[movePoints.Count - 1];
            }

            hasCalculatedTouch = false;
            movePoints.Clear();
            CalculateGravity();

            float remainingPower = CheckCollision(velocity.magnitude * Time.fixedDeltaTime);
            int loopProtection = 0;
            while (remainingPower > 0 && loopProtection < 10)
            {
                remainingPower = CheckCollision(remainingPower);
                loopProtection += 1;
            }
        }

        public void Strike(float power, Vector3 moveDir)
        {
            velocity = moveDir * speed * power;
        }

        private void CalculateGravity()
        {
            if (contactType == Grounded.Air)
            {
                velocity += Vector3.up * gravity * (Time.time - airTime);
                return;
            }
        }

        private float CheckCollision(float remainingPower)
        {
            Vector3 closestPoint, surfaceNormal;

            Vector3 referencePos = transform.position;
            if (movePoints.Count > 0)
            {
                referencePos = movePoints[^1];
            }

            if (remainingPower < .05f)
            {
                //TODO insert roll check
                Collider[] rollCheckColl = Physics.OverlapSphere(referencePos, sc.radius + 0.1f, groundMask);
                rollCheckColl.Reverse();
                foreach (Collider coll in rollCheckColl)
                {
                    if (GameObject.ReferenceEquals(coll.gameObject, this.gameObject))
                        continue;

                    if (CheckSphereExtra(coll, sc, referencePos, out closestPoint, out surfaceNormal))
                    {
                        Debug.Log("slowly stop roll");
                        return CalculateRoll(remainingPower, closestPoint, surfaceNormal);
                    }
                }

                return 0f;
            }

            RaycastHit[] hits = Physics.SphereCastAll(referencePos, sc.radius, velocity.normalized, remainingPower, groundMask);
            hits.Reverse();
            float newPower = remainingPower;

            // check raycast out, test for correct hits, test for ground (right below player) 
            foreach (RaycastHit hit in hits)
            {
                if (GameObject.ReferenceEquals(this.gameObject, hit.collider.gameObject))
                    continue;

                if (CheckSphereExtra(hit.collider, sc, hit.point, out closestPoint, out surfaceNormal) || (hit.point == Vector3.zero && CheckSphereExtra(hit.collider, sc, referencePos, out closestPoint, out surfaceNormal)))
                {
                    // Test for if ball is already moving away from the surface
                    if (Vector3.Angle(velocity, surfaceNormal) < 90f)
                    {
                        continue;
                    }
                    // Check collision layer to set contactType
                    SetGroundType(hit.collider.gameObject.layer);

                    // Add point of collision to MovePoints
                    Vector3 collidePointCenter = closestPoint + surfaceNormal * sc.radius;
                    newPower = remainingPower - (collidePointCenter - referencePos).magnitude;
                    movePoints.Add(collidePointCenter);

                    // Calculate bounce from surface normal using trigonometry to then check for either bounce or roll
                    velocity = Vector3.Reflect(velocity, surfaceNormal.normalized);

                    if ((velocity.magnitude * Mathf.Cos(Vector3.Angle(surfaceNormal, velocity.normalized) * Mathf.PI / 180f) / velocity.magnitude) >= bounceMin)
                    {
                        #region Bounce Calculations
                        // calculate velocity after bounce
                        if (hasCalculatedTouch == false)
                        {
                            switch (contactType)
                            {
                                case Grounded.Grass:
                                    velocity *= grassBounce;
                                    newPower *= grassBounce;
                                    lastBounceVelocity *= grassBounce;
                                    break;
                                case Grounded.Stone:
                                    velocity *= stoneBounce;
                                    newPower *= stoneBounce;
                                    lastBounceVelocity *= stoneBounce;
                                    break;
                                case Grounded.Ceramic:
                                    velocity *= ceramicBounce;
                                    newPower *= ceramicBounce;
                                    lastBounceVelocity *= ceramicBounce;
                                    break;
                            }
                            hasCalculatedTouch = true;
                        }

                        // Bounce strength limiter
                        // First bounce of many

                        float tempVel = velocity.magnitude;

                        if (lastBounceVelocity == 0)
                        {
                            tempVel = Mathf.Min(tempVel, velocity.magnitude);
                        }
                        else // second and rest of bounces
                        {
                            // New Bounce Pos is lower than the old bounce pos
                            if (collidePointCenter.y < lastBounceYPos)
                            {
                                lastBounceVelocity = tempVel;
                                lastBounceYPos = collidePointCenter.y;
                            }

                            if (collidePointCenter.y > lastBounceYPos)
                            {
                                lastBounceYPos = collidePointCenter.y;
                            }

                            while (tempVel > lastBounceVelocity)
                            {
                                tempVel = velocity.magnitude * allBounceStrength;
                                velocity = velocity.normalized * velocity.magnitude * allBounceStrength;
                            }
                        }

                        if (lastBounceVelocity == 0)
                            lastBounceVelocity = tempVel;
                        lastBounceVelocity = Mathf.Min(tempVel, lastBounceVelocity);

                        lastBounceYPos = collidePointCenter.y;

                        // Now recall the function by returning newPower
                        return newPower;
                        #endregion
                    }
                    else
                    {
                        Debug.Log("Rolling faster roll");
                        velocity = Vector3.ProjectOnPlane(velocity, surfaceNormal);
                        return CalculateRoll(remainingPower, closestPoint, surfaceNormal);
                    }
                }
            }

            // NO VALID COLLISIONS OR NOTHING, AIRBALL!
            if (contactType != Grounded.Air)
            {
                airTime = Time.time;
                contactType = Grounded.Air;
            }
            movePoints.Add(TestPos(remainingPower, referencePos + velocity.normalized * remainingPower));
            return 0f;
        }

        private float CalculateRoll(float remainingPower, Vector3 closestPoint, Vector3 surfaceNormal)
        {
            float newPower = remainingPower;

            Vector3 referencePos = transform.position;
            if (movePoints.Count >= 1)
                referencePos = movePoints[^1];
            Vector3 roughFinalPos = referencePos + velocity.normalized * newPower;

            if (hasCalculatedTouch == false && Vector3.Angle(Vector3.up, surfaceNormal) < 5f)
            {
                Debug.Log("Shouldnt see this on an incline");
                switch (contactType)
                {
                    case Grounded.Grass:
                        velocity *= grassFriction;
                        newPower *= grassFriction;
                        break;
                    case Grounded.Stone:
                        velocity *= stoneFriction;
                        newPower *= stoneFriction;
                        break;
                    case Grounded.Ceramic:
                        velocity *= ceramicFriction;
                        newPower *= ceramicFriction;
                        break;
                }
                hasCalculatedTouch = true;
            }

            velocity = velocity + Vector3.ProjectOnPlane(Vector3.down * -gravity * Time.fixedDeltaTime * rollMult, surfaceNormal);
            velocity = Vector3.ProjectOnPlane(velocity, surfaceNormal);
            roughFinalPos = roughFinalPos + Vector3.ProjectOnPlane(Vector3.down * -gravity * Time.fixedDeltaTime * rollMult, surfaceNormal);

            movePoints.Add(TestPos(newPower, roughFinalPos));
            return 0f;
        }

        private Vector3 TestPos(float newPower, Vector3 testPos)
        {
            Vector3 closestPoint, surfaceNormal;

            Collider[] colliders = Physics.OverlapSphere(testPos + velocity.normalized * newPower, sc.radius);
            foreach (Collider coll in colliders)
            {
                if (GameObject.ReferenceEquals(coll.gameObject, this.gameObject)) continue;

                if (CheckSphereExtra(coll, sc, testPos + velocity.normalized * newPower, out closestPoint, out surfaceNormal))
                {
                    return surfaceNormal.normalized * sc.radius + closestPoint;
                }
            }

            // new position is valid and does not need any changes
            return testPos;
        }

        private void SetGroundType(int layerNum)
        {
            if (layerNum == LayerMask.NameToLayer("Grass"))
            {
                //Debug.Log("Grass Layer");
                airTime = Mathf.Infinity;
                contactType = Grounded.Grass;
            }
            else if (layerNum == LayerMask.GetMask("Stone"))
            {
                //Debug.Log("Stone Layer");
                airTime = Mathf.Infinity;
                contactType = Grounded.Stone;
            }
            else if (layerNum == LayerMask.GetMask("Ceramic"))
            {
                //Debug.Log("Ceramic Layer");
                airTime = Mathf.Infinity;
                contactType = Grounded.Ceramic;
            }
            else
            {
                //Debug.Log("Catch Layer");
                airTime = Mathf.Infinity;
                contactType = Grounded.Ceramic;
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


        public bool CheckSphereExtra(Collider target_collider, SphereCollider sphere_collider, Vector3 reference_pos, out Vector3 closest_point, out Vector3 surface_normal)
        {
            closest_point = Vector3.zero;
            surface_normal = Vector3.zero;
            float surface_penetration_depth = 0f;
            Vector3 cast_pos = reference_pos + (velocity.normalized * -sc.radius * .5f);
            // since this will always be used with a moving sphere, would using a capsule colider be better?
            if (Physics.ComputePenetration(target_collider, target_collider.transform.position, target_collider.transform.rotation, sphere_collider, cast_pos, Quaternion.identity, out surface_normal, out surface_penetration_depth))
            {
                closest_point = cast_pos + (surface_normal * (sphere_collider.radius - surface_penetration_depth));

                surface_normal = -surface_normal;
                return true;
            }

            // move cast_pos around and test again
            cast_pos = reference_pos + Vector3.down * sc.radius * .5f;
            if (Physics.ComputePenetration(target_collider, target_collider.transform.position, target_collider.transform.rotation, sphere_collider, cast_pos, Quaternion.identity, out surface_normal, out surface_penetration_depth))
            {
                closest_point = cast_pos + (surface_normal * (sphere_collider.radius - surface_penetration_depth));

                surface_normal = -surface_normal;
                return true;
            }

            // move cast_pos around and test again
            cast_pos = reference_pos + Vector3.up * sc.radius * .5f;
            if (Physics.ComputePenetration(target_collider, target_collider.transform.position, target_collider.transform.rotation, sphere_collider, cast_pos, Quaternion.identity, out surface_normal, out surface_penetration_depth))
            {
                closest_point = cast_pos + (surface_normal * (sphere_collider.radius - surface_penetration_depth));

                surface_normal = -surface_normal;
                return true;
            }

            return false;
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
