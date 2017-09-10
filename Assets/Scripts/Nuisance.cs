using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using enums;

public class Nuisance : MonoBehaviour
{

    public bool _IsComingFromLeft = false;
    public Type _DistractionType;
    public Vector3 _WalkingVelocity;
    [Range(0.01f, 2f)]
    public float _Walkingmodifier;

    public void Update()
    {
        transform.position += _WalkingVelocity * Time.deltaTime;
    }


}

namespace enums
{
    public enum Type
    {
        normal,
        dog,
        holy,
    }
}
