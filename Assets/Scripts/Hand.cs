using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public List<GameObject> _SlapAble = new List<GameObject>();
    public void OnTriggerEnter2D(Collider2D collision)
    {
        _SlapAble.Add(collision.gameObject);
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if(_SlapAble.Contains(collision.gameObject))
        {
            _SlapAble.Remove(collision.gameObject);
        }
    }
}
