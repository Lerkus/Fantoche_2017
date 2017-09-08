using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using enums;

public class Nuisance : MonoBehaviour {

    public bool _IsComingFromLeft = false;
    public Type _DistractionType;

    //private GameMaster _MasterData;

	void Start () {
        //_MasterData = GameObject.FindGameObjectsWithTag("GameMaster")[0].GetComponent<GameMaster>();
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
