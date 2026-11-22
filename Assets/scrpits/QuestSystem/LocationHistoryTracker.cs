using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationHistoryTracker : MonoBehaviour
{
    // Start is called before the first frame update
    private readonly HashSet<LocationSO> locationsVisited = new HashSet<LocationSO>();
    void Start()
    {
        
    }

    public void RecordLocation(LocationSO locationSO)
    {
        if (locationsVisited.Add(locationSO))
        {
            Debug.Log("Just visited to"+locationSO.displayName);
        }
    }

    public bool HasVisited(LocationSO locationSO)
    {
        return locationsVisited.Contains(locationSO);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
