using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chopping : MonoBehaviour
{
    //A script which allows the player to chop food using a knife they are able to pick up

    public GameObject carrot;
    public GameObject knife;

    void Start()
    {
        Instantiate(carrot, new Vector3(9.1f, 1.4f, -4f), Quaternion.Euler(90, 0, 0));
        Instantiate(knife, new Vector3(9.75f, 1.3f, -4f), Quaternion.Euler(0, 0, 90));
    }

    void Update()
    {
        
    }
}
