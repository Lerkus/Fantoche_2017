using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public GameObject _SlapAble = null;
    public void OnTriggerEnter2D(Collider2D collision)
    {
        _SlapAble = collision.gameObject;
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if(_SlapAble = collision.gameObject)
        {
            _SlapAble = null;
        }
    }
}
