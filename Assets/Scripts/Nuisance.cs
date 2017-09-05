using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using enums;

public class Nuisance : MonoBehaviour {

    public bool IsComingFromLeft = false;
    public Type DistractionType;
    private GameMaster MasterData;

    public void RandomReset()
    {

    }

	void Start () {
        MasterData = GameObject.FindGameObjectsWithTag("GameMaster")[0].GetComponent<GameMaster>();
	}
	
	void Update () {
		
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
