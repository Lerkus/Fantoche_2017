using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkAppearance : MonoBehaviour
{

    public int _WisdomLevel = -1;
    public Sprite[] _MonkStageSprites;
    [Range(0.01f, 1f)]
    public float _TimeUntilSpriteFlip;
    public Sprite[] _AlternateMonkStageSprites;

    private SpriteRenderer _MonkRenderer;
    private Coroutine _TimerSpriteFlip;
    private bool _AlternateSprite = false;

    public void Start()
    {
        _MonkRenderer = gameObject.GetComponent<SpriteRenderer>();
        _TimerSpriteFlip = StartCoroutine(SpriteFlip());
    }

    public void Update()
    {
        MonkSpriteUpdate();
    }
    public IEnumerator SpriteFlip()
    {
        while (true)
        {
            yield return new WaitForSeconds(_TimeUntilSpriteFlip);
            _AlternateSprite = !_AlternateSprite;
        }
    }
    private void MonkSpriteUpdate()
    {
        if (_WisdomLevel >= 0 && _WisdomLevel < _MonkStageSprites.GetLength(0))
        {
            if (_AlternateSprite)
            {
                _MonkRenderer.sprite = _AlternateMonkStageSprites[_WisdomLevel];
            }
            else
            {
                _MonkRenderer.sprite = _MonkStageSprites[_WisdomLevel];
            }
        }
        else
        {
            if (_AlternateSprite)
            {
                _MonkRenderer.sprite = _AlternateMonkStageSprites[0];
            }
            else
            {
                _MonkRenderer.sprite = _MonkStageSprites[0];
            }
        }
    }
}
