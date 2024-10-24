using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace gameracers.Movement
{
    public class TestMover : MonoBehaviour
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

            CheckCollisions();
        }

        public void Strike(float power, Vector3 facing)
        {
            velocity = facing * power;
        }

        private void CheckCollisions()
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

            Vector3 referencePoint = transform.position;
            if (movePoints.Count > 0)
                referencePoint = movePoints[^1];

            float trueDist = 0; // dist from last known pos to current collisionPoint
            Vector3 collisionPoint = Vector3.zero;

            RaycastHit[] hits = Physics.SphereCastAll(referencePoint, sc.radius, velocity.normalized, remainingPower);

            foreach (RaycastHit hit in hits)
            {
                if (GameObject.ReferenceEquals(this.gameObject, hit.collider.gameObject))
                    continue;

                // test if raycast is behind the ball
                if ((transform.position - hit.collider.ClosestPoint(transform.position)).magnitude >
                    (transform.position - velocity.normalized * sc.radius - hit.collider.ClosestPoint(transform.position)).magnitude)
                    continue;

                if ((groundMask & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    #region Bounce Calculation
                    Debug.Log("Bounce");

                    Vector3 localTravel = hit.collider.ClosestPoint(referencePoint) - referencePoint;

                    collisionPoint = referencePoint + localTravel.normalized * (localTravel.magnitude - sc.radius); // world position of the ball at point of impact. 

                    float angle = Vector3.Angle(velocity, (referencePoint - hit.collider.ClosestPoint(referencePoint)).normalized);

                    float tempVel = velocity.magnitude;

                    velocity = Vector3.Reflect(velocity, (referencePoint - hit.collider.ClosestPoint(referencePoint)).normalized);

                    // set contactType
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grass"))
                    {
                        contactType = Grounded.Grass;
                    }
                    else if (hit.collider.gameObject.layer == LayerMask.GetMask("Stone"))
                    {
                        contactType = Grounded.Stone;
                    }
                    else if (hit.collider.gameObject.layer == LayerMask.GetMask("Ceramic"))
                    {
                        contactType = Grounded.Ceramic;
                    }
                    else
                    {
                        contactType = Grounded.Ceramic;
                    }

                    //TODO calculate new velocity
                    // if current yPos is greater than lastYPos, the resulting velocity's magnitude will be less, otherwise, process it as regular
                    if (lastBounceVelocity != 0f && collisionPoint.y >= lastBounceYPos)
                    {
                        velocity = velocity.normalized * (lastBounceVelocity * allBounceStrength);
                    }

                    while (lastBounceVelocity != 0 && velocity.magnitude > lastBounceVelocity)
                    {
                        velocity = new Vector3(velocity.x, velocity.y * allBounceStrength, velocity.z);
                    }

                    lastBounceVelocity = tempVel;
                    lastBounceYPos = collisionPoint.y;
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

                    // TODO how to stop or properly manage small small bounces? if it is done the old way, any small upward momentum would get cancelled out, meaning rolling uphill was not possible. is this even a need?

                    while (lastBounceVelocity != 0 && velocity.magnitude > lastBounceVelocity)
                    {
                        velocity = velocity * allBounceStrength;
                    }
                    #endregion

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

        }
    }
}
