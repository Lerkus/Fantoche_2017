using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;

public class GameMaster : MonoBehaviour
{
    #region Variables
    [Header("Startvalues:")]
    [Header("muss genau ein Prefab pro Gegnertyp haben.")]
    public GameObject[] _PrefabNuisance;
    public float[] _PrefabRandomWeight;

    public float[] _TimeNeededForStage;

    public GameObject _CamObject;
    public GameObject _SageObject;
    public Hand _LeftHand;
    public Hand _RightHand;

    public Vector2 _SpawnpointLeft;
    public Vector2 _SpawnpointRight;
    public Vector2 _SagePointLeft;
    public Vector2 _SagePointRight;
    public float _TranquilDistance;

    public Vector3 _NuisanceWalkingVelocity;
    public float _TimeSpawnCycle;
    [Range(0.01f, 1f)]
    public float _SlowmoTimePercentage;
    [Range(0.01f, 1f)]
    public float _TimeUntilHandDisappears;
    public float _TimeUntilThrowReady;

    public float _AmountOfSlapsNeeded;
    public float _MaxTimeForBarricateSlapping;

    [Range(0.01f, 1)]
    public float _PercentageIncreasePerStage;
    public GameObject _Startscreen;

    //Startwerte der sich je nach Schwierigkeitändernde Parameter.
    private Vector3 _NuisanceWalkingVelocity_AtStart;
    private float _TimeSpawnCycle_AtStart;
    private float _SlowmoTimePercentage_AtStart;
    private float _TimeUntilThrowReady_AtStart;
    private float _AmountOfSlapsNeeded_AtStart;
    private float _MaxTimeForBarricateSlapping_AtStart;

    private List<GameObject> _WalkingObjectsLeft = new List<GameObject>();
    private List<GameObject> _WalkingObjectsRight = new List<GameObject>();
    private List<GameObject> _ObjectPool = new List<GameObject>();
    private int _StartFillingCountObjectPool = 10;

    private Coroutine _SpawnerTimer;

    private Vector3 _OriginalCamPosition;
    private Hand _BusyHand;

    private bool _IsThrowing = false;
    private bool _ReadyToThrow = false;
    private Coroutine _TimerUntilThrowOkay;

    private bool _IsBarricateSlapping = false;
    private int _SlappingCounter = 0;
    private Coroutine _TimerForSlappingBarricate;

    private float _TimestampLastHit = 0;

    private List<Coroutine> _GotHit = new List<Coroutine>();
    private Coroutine _LeftHandAppearTimer;
    private Coroutine _RightHandAppearTimer;
    private Animator _WisdomDisplay;
    private int _WisdomLevelBuffer = 0;
    private bool _GameHasStarted = false;
    #endregion
    #region Properties
    public GameObject AnotherWalkingObject
    {
        get
        {
            GameObject buffer;

            if (_ObjectPool.Count == 0)
            {
                buffer = GameObject.Instantiate(RandomPrefab);
                buffer.SetActive(false);
                _ObjectPool.Add(buffer);
            }

            buffer = _ObjectPool[Random.Range(0, _ObjectPool.Count - 1)];
            _ObjectPool.Remove(buffer);
            Nuisance bufferData = buffer.GetComponent<Nuisance>();
            bufferData._IsComingFromLeft = Random.value > 0.5f;

            if (bufferData._IsComingFromLeft)
            {
                buffer.transform.position = _SpawnpointLeft;
                buffer.GetComponent<SpriteRenderer>().flipX = false;
                bufferData._WalkingVelocity = _NuisanceWalkingVelocity;
                _WalkingObjectsLeft.Add(buffer);
            }
            else
            {
                buffer.transform.position = _SpawnpointRight;
                buffer.GetComponent<SpriteRenderer>().flipX = true;
                bufferData._WalkingVelocity = _NuisanceWalkingVelocity * -1;
                _WalkingObjectsRight.Add(buffer);
            }

            buffer.SetActive(true);
            return buffer;
        }
    }
    private GameObject RandomPrefab
    {
        get
        {
            int id = _PrefabRandomWeight.GetLength(0) - 1;
            float sumUntilId = 0;
            float randomNumber = 0;

            for (int i = 0; i < _PrefabRandomWeight.GetLength(0); i++)
            {
                sumUntilId += _PrefabRandomWeight[i];
            }

            randomNumber = Random.Range(0.0f, sumUntilId);

            while (randomNumber < sumUntilId)
            {
                sumUntilId -= _PrefabRandomWeight[id];
                id--;
            }

            return _PrefabNuisance[id + 1];
        }
    }
    private int WisdomLevel
    {
        get
        {
            int stage = 0;
            float TimeSinceLastHit = Time.time - _TimestampLastHit;
            for (int i = 0; i < _TimeNeededForStage.GetLength(0); i++)
            {
                if (TimeSinceLastHit > _TimeNeededForStage[i])
                {
                    stage = i;
                }
            }

                _WisdomLevelBuffer = stage;
                return _WisdomLevelBuffer;
        }
        set
        {
            if(value >= 0 && value < _TimeNeededForStage.GetLength(0))
            {
                _TimestampLastHit = Time.time - _TimeNeededForStage[value];
            }
        }
    }
    #endregion
    #region EngineFunctions
    public void Start()
    {
        GameObject buffer;
        for (int i = 0; i < _StartFillingCountObjectPool; i++)
        {
            buffer = GameObject.Instantiate(RandomPrefab);
            buffer.SetActive(false);
            _ObjectPool.Add(buffer);
        }
        _SpawnerTimer = StartCoroutine(SpawnCycle());
        _TimestampLastHit = Time.time;

        _NuisanceWalkingVelocity_AtStart = _NuisanceWalkingVelocity;
        _TimeSpawnCycle_AtStart = _TimeSpawnCycle;
        _SlowmoTimePercentage_AtStart = _SlowmoTimePercentage;
        _TimeUntilThrowReady_AtStart = _TimeUntilThrowReady;
        _AmountOfSlapsNeeded_AtStart = _AmountOfSlapsNeeded;
        _MaxTimeForBarricateSlapping_AtStart = _MaxTimeForBarricateSlapping;

        _WisdomDisplay = _SageObject.GetComponent<Animator>();
    }
    public void Update()
    {
        PlayerInput();
        if (_GameHasStarted)
        {
            GotHitUpdate();
            TimeUpdate();
        }
    }
    #endregion
    #region Coroutines
    public IEnumerator SpawnCycle()
    {
        GameObject buffer;
        while (true)
        {
            yield return new WaitForSeconds(_TimeSpawnCycle);
            if (_GameHasStarted)
            {
                buffer = AnotherWalkingObject;
            }
        }
    }
    public IEnumerator StopwatchThrow()
    {
        yield return new WaitForSecondsRealtime(_TimeUntilThrowReady);
        _ReadyToThrow = true;
    }
    public IEnumerator StopwatchBarricateSlapping()
    {
        yield return new WaitForSecondsRealtime(_MaxTimeForBarricateSlapping);
        SlapBarricateStop(_BusyHand);
    }
    public IEnumerator HitBlink()
    {
        _SageObject.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255, _SageObject.GetComponent<SpriteRenderer>().color.a / 2.0f);
        yield return new WaitForSeconds(0.2f);
        _SageObject.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255, _SageObject.GetComponent<SpriteRenderer>().color.a * 2.0f);
        Coroutine Buffer = _GotHit[0];
        _GotHit.Remove(Buffer);
        StopCoroutine(Buffer);
    }
    public IEnumerator DisapearTimer(Hand WhichHand)
    {
        yield return new WaitForSeconds(_TimeUntilHandDisappears);

        WhichHand.gameObject.transform.GetChild(0).gameObject.SetActive(false);
        if (WhichHand == _LeftHand && _LeftHandAppearTimer != null)
        {
            StopCoroutine(_LeftHandAppearTimer);
        }
        else if (WhichHand == _RightHand && _RightHandAppearTimer != null)
        {
            StopCoroutine(_RightHandAppearTimer);
        }
    }
    #endregion
    private void GotHitUpdate()
    {
        List<GameObject> Buffer = new List<GameObject>();
        bool shouldBePunished = false;
        for(int i = 0; i < _WalkingObjectsLeft.Count;i++)
        {
            if ((_WalkingObjectsLeft[i].transform.position - _SageObject.transform.position).sqrMagnitude < _TranquilDistance * _TranquilDistance)
            {
                if (_IsThrowing)
                {
                    ObjectThrowStop(_BusyHand);
                }
                if (_IsBarricateSlapping)
                {
                    SlapBarricateStop(_BusyHand);
                }
                shouldBePunished = true;
                Buffer.Add(_WalkingObjectsLeft[i]);
            }
        }

        for (int i = 0; i < _WalkingObjectsRight.Count; i++)
        {
            if ((_WalkingObjectsRight[i].transform.position - _SageObject.transform.position).sqrMagnitude < _TranquilDistance * _TranquilDistance)
            {
                if (_IsThrowing)
                {
                    ObjectThrowStop(_BusyHand);
                }
                if (_IsBarricateSlapping)
                {
                    SlapBarricateStop(_BusyHand);
                }
                shouldBePunished = true;
                Buffer.Add(_WalkingObjectsRight[i]);
            }
        }

        for (int i = 0; i < Buffer.Count; i++)
        {
            RemoveWalkingObject(Buffer[i]);
        }
        if (shouldBePunished)
        {
            Penalty();
        }
    }
    private void RemoveNearestLeftNuisance()
    {
        GameObject buffer;
        if (_WalkingObjectsLeft.Count > 0)
        {
            buffer = _WalkingObjectsLeft[0];
            buffer.SetActive(false);
            _WalkingObjectsLeft.Remove(buffer);
            _ObjectPool.Add(buffer);
        }
    }
    private void RemoveNearestRightNuisance()
    {
        GameObject buffer;
        if (_WalkingObjectsRight.Count > 0)
        {
            buffer = _WalkingObjectsRight[0];
            buffer.SetActive(false);
            _WalkingObjectsRight.Remove(buffer);
            _ObjectPool.Add(buffer);
        }
    }
    private void RemoveWalkingObject(GameObject WalkingObject)
    {
        if (_WalkingObjectsLeft.Contains(WalkingObject))
        {
            WalkingObject.SetActive(false);
            _WalkingObjectsLeft.Remove(WalkingObject);
            _ObjectPool.Add(WalkingObject);
        }
        if (_WalkingObjectsRight.Contains(WalkingObject))
        {
            WalkingObject.SetActive(false);
            _WalkingObjectsRight.Remove(WalkingObject);
            _ObjectPool.Add(WalkingObject);
        }
    }
    private void Penalty()
    {
        if(WisdomLevel - 1 != -1)
        {
        WisdomLevel = WisdomLevel -1;
        Coroutine Buffer = StartCoroutine(HitBlink());
        _GotHit.Add(Buffer);
        }
        else
        {
            StopGame();
        }
    }
    private void PlayerInput()
    {
        if(!_GameHasStarted && (Input.GetButtonDown("Left") || Input.GetButtonDown("Right")))
        {
            StartGame();
        }

        if (!IsBusySlapping() && (Input.GetButtonDown("Left") || Input.GetButtonDown("Right")))
        {
            InitialSlaps();
        }

        else if (_IsThrowing)
        {
            if ((Input.GetButton("Left") && _BusyHand == _LeftHand) || (Input.GetButton("Right") && _BusyHand == _RightHand))
            {
            }
            else
            {
                ObjectThrowStop(_BusyHand);
            }
        }
        else if (_IsBarricateSlapping)
        {
            if ((Input.GetButtonDown("Left") && _BusyHand == _LeftHand) || (Input.GetButtonDown("Right") && _BusyHand == _RightHand))
            {
                _SlappingCounter++;
                _BusyHand.gameObject.GetComponent<Animator>().SetTrigger("throw");
                if (_AmountOfSlapsNeeded <= _SlappingCounter)
                {
                    if (_BusyHand == _LeftHand)
                    {
                        RemoveNearestLeftNuisance();
                    }
                    else
                    {
                        RemoveNearestRightNuisance();
                    }
                    SlapBarricateStop(_BusyHand);
                }
            }
        }
    }
    private void InitialSlaps()
    {
        if (Input.GetButtonDown("Left"))
        {
            DecideSlap(_LeftHand);
        }
        if (Input.GetButtonDown("Right"))
        {
            DecideSlap(_RightHand);
        }
    }
    private bool IsBusySlapping()
    {
        return _IsThrowing || _IsBarricateSlapping;
    }
    private void DecideSlap(Hand WhichHand)
    {
        if (WhichHand != null && WhichHand._SlapAble.Count > 0)
        {
            Nuisance BufferNuisance = WhichHand._SlapAble[0].GetComponent<Nuisance>();
            enums.Type BufferType = BufferNuisance._DistractionType;
            switch (BufferType)
            {
                case enums.Type.normal:
                    NormalSlap(WhichHand);
                    break;

                case enums.Type.dog:
                    ObjectThrowStart(WhichHand);
                    break;

                case enums.Type.holy:
                    SlapBarricateStart(WhichHand);
                    break;
                default:
                    break;
            }
        }
    }
    private void NormalSlap(Hand WhichHand)
    {
        GameObject Buffer = WhichHand._SlapAble[0];
        RemoveWalkingObject(WhichHand._SlapAble[0]);
        WhichHand._SlapAble.Remove(Buffer);
        LetHandAppear(WhichHand);
    }
    private void ObjectThrowStart(Hand WhichHand)
    {
        StartSlowmoZoom(WhichHand);
        _TimerUntilThrowOkay = StartCoroutine(StopwatchThrow());
        _IsThrowing = true;
        _BusyHand = WhichHand;
        LetHandAppear(WhichHand);
        WhichHand.gameObject.GetComponent<Animator>().SetTrigger("throw");
    }
    private void ObjectThrowStop(Hand WhichHand)
    {
        StopSlowmoZoom();
        if (_ReadyToThrow)
        {
            if (_BusyHand == _LeftHand)
            {
                RemoveNearestLeftNuisance();
            }
            else
            {
                RemoveNearestRightNuisance();
            }
            _ReadyToThrow = false;
        }
        if (_TimerUntilThrowOkay != null)
        {
            StopCoroutine(_TimerUntilThrowOkay);
            _TimerUntilThrowOkay = null;
        }
        _IsThrowing = false;
        _BusyHand = null;
    }
    private void SlapBarricateStart(Hand WhichHand)
    {
        StartSlowmoZoom(WhichHand);
        _IsBarricateSlapping = true;
        _BusyHand = WhichHand;
        _SlappingCounter = 0;
        _TimerForSlappingBarricate = StartCoroutine(StopwatchBarricateSlapping());
        LetHandAppear(WhichHand);
        WhichHand.gameObject.GetComponent<Animator>().SetTrigger("slap");
    }
    private void SlapBarricateStop(Hand WhichHand)
    {
        StopSlowmoZoom();
        _IsBarricateSlapping = false;
        _BusyHand = null;
        _SlappingCounter = 0;
        if (_TimerForSlappingBarricate != null)
        {
            StopCoroutine(_TimerForSlappingBarricate);
        }
    }
    private void TimeUpdate()
    {
        int BufferLevel = WisdomLevel;
        float BufferDevelopment = 1;

        for (int i = 0; i < BufferLevel; i++)
        {
            BufferDevelopment *= 1 + _PercentageIncreasePerStage;
        }

        _NuisanceWalkingVelocity = _NuisanceWalkingVelocity_AtStart / BufferDevelopment;
        _TimeSpawnCycle = _TimeSpawnCycle_AtStart / BufferDevelopment;
        _SlowmoTimePercentage = _SlowmoTimePercentage_AtStart * BufferDevelopment;
        _TimeUntilThrowReady = _TimeUntilThrowReady_AtStart * BufferDevelopment;
        _AmountOfSlapsNeeded = _AmountOfSlapsNeeded_AtStart * BufferDevelopment;
        _MaxTimeForBarricateSlapping = _MaxTimeForBarricateSlapping_AtStart / BufferDevelopment;

        _WisdomDisplay.SetInteger("WisdomLevel", BufferLevel);
        if(WisdomLevel == _TimeNeededForStage.GetLength(0) - 1)
        {
            StopGame();
        }
    }
    private void LetHandAppear(Hand WhichHand)
    {
        WhichHand.gameObject.transform.GetChild(0).gameObject.SetActive(true);
        Coroutine Buffer = StartCoroutine(DisapearTimer(WhichHand));

        if (WhichHand == _LeftHand)
        {
            if (_LeftHandAppearTimer != null)
            {
                StopCoroutine(_LeftHandAppearTimer);
                _LeftHandAppearTimer = Buffer;
            }
        }
        else if (WhichHand == _RightHand)
        {
            if (_RightHandAppearTimer != null)
            {
                StopCoroutine(_RightHandAppearTimer);
                _RightHandAppearTimer = Buffer;
            }
        }

    }
    private void StartSlowmoZoom(Hand WhichHand)
    {
        Time.timeScale = _SlowmoTimePercentage;
        /*
        _OriginalCamPosition = _CamObject.transform.position;
        _CamObject.transform.position = Vector3.Scale(WhichHand.gameObject.transform.position, new Vector3(1, 0, 0));
        _CamObject.transform.position += Vector3.Scale(_OriginalCamPosition, new Vector3(0, 1, 1));
        */
    }
    private void StopSlowmoZoom()
    {
        Time.timeScale = 1;
        //_CamObject.transform.position = _OriginalCamPosition;
    }
    private void StartGame()
    {
        _Startscreen.SetActive(false);
        _GameHasStarted = true;
        WisdomLevel = 0;
        _WisdomDisplay.SetInteger("WisdomLevel", WisdomLevel);
    }
    private void StopGame()
    {
        _Startscreen.SetActive(true);
        _GameHasStarted = false;
        int buffer = _WalkingObjectsLeft.Count;
        for(int i = 0; i < buffer; i++)
        {
            RemoveNearestLeftNuisance();
        }
        buffer = _WalkingObjectsRight.Count;
        for (int i = 0; i < buffer; i++)
        {
            RemoveNearestRightNuisance();
        }
        _WisdomDisplay.SetInteger("WisdomLevel", WisdomLevel);
    }
}
