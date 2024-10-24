using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventListener : MonoBehaviour
{
    public delegate void OnBallInHole(GameObject entity, string holeID = null);
    public static event OnBallInHole onBallInHole;

    public delegate void OnBallHit(GameObject entity);
    public static event OnBallHit onBallHit;

    public static void BallHasSunk(GameObject entity, string holeID = null)
    {
        if (onBallInHole != null)
            onBallInHole(entity, holeID);
    }

    public static void BallHit(GameObject entity)
    {
        if (onBallHit != null)
            onBallHit(entity);
    }
}
