using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CircleDatabase", menuName = "Scriptable Objects/Circle Database")]
public class CircleDatabase : ScriptableObject
{
    public CircleDefinition[] circles;

    public CircleDefinition GetByNumber(int circleNumber)
    {
        if (circles == null)
        {
            return null;
        }

        return Array.Find(circles, circle => circle != null && circle.circleNumber == circleNumber);
    }
}
