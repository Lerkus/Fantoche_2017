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
    public Vector2 _SpawnpointLeft;
    public Vector2 _SpawnpointRight;
    public Vector2 _SagePoint;

    private List<GameObject> _WalkingObjects = new List<GameObject>();
    private List<GameObject> _ObjectPool = new List<GameObject>();
    private int _StartFillingCountObjectPool = 10;
    private Coroutine _SpawnerTimer;
    private bool _ShouldSpawn = false;
    private bool _ShouldWalk = false;
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

            //TODO------Random Objekt rausnehmen einfügen.
            buffer = _ObjectPool.Pop();
            Nuisance bufferData = buffer.GetComponent<Nuisance>();

            _WalkingObjects.Add(buffer);
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
            _ObjectPool.Push(buffer);
        }
    }

    public void Update()
    {

    }
    #endregion
    public void RemoveNearestNuisance()
    {
        GameObject buffer;
        if (_WalkingObjects.Count > 0)
        {
            buffer = _WalkingObjects[0];
            buffer.SetActive(false);
            _ObjectPool.Add(buffer);
        }
    }
}
