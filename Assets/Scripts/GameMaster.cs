using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    #region Variables
    [Header("muss genau ein Prefab pro Gegnertyp haben.")]
    public GameObject[] _PrefabNuisance;
    public float[] _PrefabRandomWeight;
    public GameObject _SageObject;
    public Hand LeftHand;
    public Hand RightHand;

    public Vector2 _SpawnpointLeft;
    public Vector2 _SpawnpointRight;
    public Vector2 _SagePointLeft;
    public Vector2 _SagePointRight;

    public float _TimeFromSpawnToSage;
    public float _TimeSpawnCycle;

    private List<GameObject> _WalkingObjects = new List<GameObject>();
    private List<GameObject> _ObjectPool = new List<GameObject>();
    private int _StartFillingCountObjectPool = 10;

    private Coroutine _SpawnerTimer;
    private bool _ShouldSpawn = false;
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
            _WalkingObjects.Add(buffer);
            Nuisance bufferData = buffer.GetComponent<Nuisance>();
            bufferData._IsComingFromLeft = Random.value > 0.5f;
            if (bufferData._IsComingFromLeft)
            {
                buffer.transform.position = _SpawnpointLeft;
                buffer.GetComponent<SpriteRenderer>().flipX = false;
            }
            else
            {
                buffer.transform.position = _SpawnpointRight;
                buffer.GetComponent<SpriteRenderer>().flipX = false;
            }
            bufferData._SpawnedTimeStamp = Time.timeSinceLevelLoad;
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
    }
    public void Update()
    {
        PositionUpdate();
        while (IsNearestObjectToNear())
        {
            Penalty();
            RemoveNearestNuisance();
        }
        PlayerInput();
        /*
        if (Input.anyKey)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
        */
    }
    #endregion

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
    public void RemoveNearestNuisance()
    {
        GameObject buffer;
        if (_WalkingObjects.Count > 0)
        {
            buffer = _WalkingObjects[0];
            buffer.SetActive(false);
            _WalkingObjects.Remove(buffer);
            _ObjectPool.Add(buffer);
        }
    }
    private void RemoveWalkingObject(GameObject WalkingObject)
    {
        if (_WalkingObjects.Contains(WalkingObject))
        {
            WalkingObject.SetActive(false);
            _WalkingObjects.Remove(WalkingObject);
            _ObjectPool.Add(WalkingObject);
        }
    }
    private void PositionUpdate()
    {
        Nuisance data;
        float percentageWalked;
        foreach (GameObject NoisyObject in _WalkingObjects)
        {
            data = NoisyObject.GetComponent<Nuisance>();
            percentageWalked = (Time.timeSinceLevelLoad - data._SpawnedTimeStamp) / _TimeFromSpawnToSage;

            if (data._IsComingFromLeft)
            {
                NoisyObject.transform.position = Vector3.Lerp(_SpawnpointLeft, _SagePointLeft, percentageWalked);
            }
            else
            {
                NoisyObject.transform.position = Vector3.Lerp(_SpawnpointRight, _SagePointRight, percentageWalked);
            }
        }
    }

    private bool IsNearestObjectToNear()
    {
        if (_WalkingObjects.Count > 0)
        {
            if ((Time.timeSinceLevelLoad - _WalkingObjects[0].GetComponent<Nuisance>()._SpawnedTimeStamp) / _TimeFromSpawnToSage >= 1)
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
    }
    private void PlayerInput()
    {
        if(Input.GetAxisRaw("Left") == 1)
        {
            if(LeftHand._SlapAble != null)
            {
                RemoveWalkingObject(LeftHand._SlapAble);
                LeftHand._SlapAble = null;
            }
        }
        if (Input.GetAxisRaw("Right") == 1)
        {
            if (RightHand._SlapAble != null)
            {
                RemoveWalkingObject(RightHand._SlapAble);
                RightHand._SlapAble = null;
            }
        }
    }
}
