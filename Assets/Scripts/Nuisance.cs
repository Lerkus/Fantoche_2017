using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using enums;

public class Nuisance : MonoBehaviour {

    public bool _IsComingFromLeft = false;
    public Type _DistractionType;
    public Vector3 _WalkingVelocity;
    [Range(0.01f, 2f)]
    public float _Walkingmodifier;
    public Sprite[] _SpritesForAnimation;
    [Range(0.01f,1f)]
    public float _TimeUntilSpriteFlip;

    private GameMaster _MasterData;
    private int _AnimationCounter = 0;
    private Coroutine _AnimationTimer;
    private SpriteRenderer _Renderer;

	public void Start () {
        _MasterData = GameObject.FindGameObjectsWithTag("GameMaster")[0].GetComponent<GameMaster>();
        _Renderer = gameObject.GetComponent<SpriteRenderer>();
        _AnimationTimer = StartCoroutine(NextSpriteCounter());
    }

    public void Update()
    {
        transform.position += _WalkingVelocity * Time.deltaTime;
        Animate();
    }

    public IEnumerator NextSpriteCounter()
    {
        while (true)
        {
            yield return new WaitForSeconds(_TimeUntilSpriteFlip);
            _AnimationCounter++;
            if(_AnimationCounter >= _SpritesForAnimation.GetLength(0))
            {
                _AnimationCounter = 0;
            }
        }
    }

    private void Animate()
    {
        _Renderer.sprite = _SpritesForAnimation[_AnimationCounter];
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
