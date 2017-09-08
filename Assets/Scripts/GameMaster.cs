using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public Text _WisdomDisplay;

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
    private bool _ShouldSpawn = false;

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
                _WalkingObjectsLeft.Add(buffer);
            }
            else
            {
                buffer.transform.position = _SpawnpointRight;
                buffer.GetComponent<SpriteRenderer>().flipX = true;
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
        _ShouldSpawn = true;

        _NuisanceWalkingVelocity_AtStart = _NuisanceWalkingVelocity;
        _TimeSpawnCycle_AtStart = _TimeSpawnCycle;
        _SlowmoTimePercentage_AtStart = _SlowmoTimePercentage;
        _TimeUntilThrowReady_AtStart = _TimeUntilThrowReady;
        _AmountOfSlapsNeeded_AtStart = _AmountOfSlapsNeeded;
        _MaxTimeForBarricateSlapping_AtStart = _MaxTimeForBarricateSlapping;
    }
    public void Update()
    {
        PositionUpdate();
        PlayerInput();
        GotHitUpdate();
        TimeUpdate();
    }
    #endregion
    #region Coroutines
    public IEnumerator SpawnCycle()
    {
        GameObject buffer;
        while (true)
        {
            yield return new WaitForSeconds(_TimeSpawnCycle);
            if (_ShouldSpawn)
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
        else if(WhichHand == _RightHand && _RightHandAppearTimer != null)
        {
            StopCoroutine(_RightHandAppearTimer);
        }
    }
    #endregion
    private void GotHitUpdate()
    {
        while (IsNearestLeftObjectToNear())
        {
            if (_IsThrowing)
            {
                ObjectThrowStop(_BusyHand);
            }
            if (_IsBarricateSlapping)
            {
                SlapBarricateStop(_BusyHand);
            }
            Penalty();
            RemoveNearestLeftNuisance();
        }
        while (IsNearestRightObjectToNear())
        {
            if (_IsThrowing)
            {
                ObjectThrowStop(_BusyHand);
            }
            if (_IsBarricateSlapping)
            {
                SlapBarricateStop(_BusyHand);
            }
            Penalty();
            RemoveNearestRightNuisance();
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
    private void PositionUpdate()
    {
        Nuisance data;
        foreach (GameObject NoisyObject in _WalkingObjectsLeft)
        {
            data = NoisyObject.GetComponent<Nuisance>();
            NoisyObject.transform.position += _NuisanceWalkingVelocity * Time.deltaTime;

        }

        foreach (GameObject NoisyObject in _WalkingObjectsRight)
        {
            data = NoisyObject.GetComponent<Nuisance>();

            NoisyObject.transform.position += _NuisanceWalkingVelocity * Time.deltaTime * -1;
        }

    }

    private bool IsNearestLeftObjectToNear()
    {
        if (_WalkingObjectsLeft.Count > 0)
        {
            if ((_SageObject.transform.position - _WalkingObjectsLeft[0].transform.position).sqrMagnitude < _TranquilDistance * _TranquilDistance)
            {
                return true;
            }
        }
        return false;
    }
    private bool IsNearestRightObjectToNear()
    {
        if (_WalkingObjectsRight.Count > 0)
        {
            if ((_SageObject.transform.position - _WalkingObjectsRight[0].transform.position).sqrMagnitude < _TranquilDistance * _TranquilDistance)
            {
                return true;
            }
        }
        return false;
    }
    private void Penalty()
    {
        //TODO: implement penalty here.
        //use the stats of the closest Object.
        Debug.Log("Let me read my magazins in peace!");
        _TimestampLastHit = Time.time;
        Coroutine Buffer = StartCoroutine(HitBlink());
        _GotHit.Add(Buffer);
    }
    private void PlayerInput()
    {
        if (!IsBusySlapping() && (Input.GetButtonDown("Left") || Input.GetButtonDown("Right")))
        {
            InitialSlaps();
        }

        else if (_IsThrowing)
        {
            if ((Input.GetButton("Left") && _BusyHand == _LeftHand) || (Input.GetButton("Right") && _BusyHand == _RightHand))
            {
                //TODO: aufladen effekte hier einbauen.
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
        Time.timeScale = _SlowmoTimePercentage;
        _OriginalCamPosition = _CamObject.transform.position;
        _CamObject.transform.position = Vector3.Scale(WhichHand.gameObject.transform.position, new Vector3(1, 1, 0));
        _CamObject.transform.position += Vector3.Scale(_OriginalCamPosition, new Vector3(0, 0, 1));
        _TimerUntilThrowOkay = StartCoroutine(StopwatchThrow());
        _IsThrowing = true;
        _BusyHand = WhichHand;
        LetHandAppear(WhichHand);
    }
    private void ObjectThrowStop(Hand WhichHand)
    {
        Time.timeScale = 1;
        _CamObject.transform.position = _OriginalCamPosition;
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
        Time.timeScale = _SlowmoTimePercentage;
        _OriginalCamPosition = _CamObject.transform.position;
        _CamObject.transform.position = Vector3.Scale(WhichHand.gameObject.transform.position, new Vector3(1, 1, 0));
        _CamObject.transform.position += Vector3.Scale(_OriginalCamPosition, new Vector3(0, 0, 1));
        _IsBarricateSlapping = true;
        _BusyHand = WhichHand;
        _SlappingCounter = 0;
        _TimerForSlappingBarricate = StartCoroutine(StopwatchBarricateSlapping());
        LetHandAppear(WhichHand);
    }
    private void SlapBarricateStop(Hand WhichHand)
    {
        Time.timeScale = 1;
        _CamObject.transform.position = _OriginalCamPosition;
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
        int stage = 0;
        float TimeSinceLastHit = Time.time - _TimestampLastHit;
        for (int i = 0; i < _TimeNeededForStage.GetLength(0); i++)
        {
            if (TimeSinceLastHit > _TimeNeededForStage[i])
            {
                stage = i;
            }
        }

        _NuisanceWalkingVelocity = _NuisanceWalkingVelocity_AtStart;
        _TimeSpawnCycle = _TimeSpawnCycle_AtStart;
        _SlowmoTimePercentage = _SlowmoTimePercentage_AtStart;
        _TimeUntilThrowReady = _TimeUntilThrowReady_AtStart;
        _AmountOfSlapsNeeded = _AmountOfSlapsNeeded_AtStart;
        _MaxTimeForBarricateSlapping = _MaxTimeForBarricateSlapping_AtStart;

        //TODO Übergang von einem SageLevel zum nächsten.
        for (int i = 0; i < stage; i++)
        {
            _NuisanceWalkingVelocity /= 1 + _PercentageIncreasePerStage;
            _TimeSpawnCycle /= 1 + _PercentageIncreasePerStage;
            _SlowmoTimePercentage *= 1 + _PercentageIncreasePerStage;
            _TimeUntilThrowReady *= 1 + _PercentageIncreasePerStage;
            _AmountOfSlapsNeeded *= 1 + _PercentageIncreasePerStage;
            _MaxTimeForBarricateSlapping /= 1 + _PercentageIncreasePerStage;
        }

        _WisdomDisplay.text = "" + stage;
    }
    private void LetHandAppear(Hand WhichHand)
    {
        WhichHand.gameObject.transform.GetChild(0).gameObject.SetActive(true);
        Debug.Log(WhichHand.gameObject.transform.childCount);
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
}
