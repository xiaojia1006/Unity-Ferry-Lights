using UnityEngine;
public sealed class RPG2Point : MonoBehaviour
{
    public enum Kind {Board,Camp,Mushroom,Pumpkin,Timber,Bridge,Beacon,Treasure,Fishing,Sheep,Inn,Guide}
    public Kind kind;
    public string title,description;
    public int number;
    public GameObject glow;
    public void SetLit(bool lit){if(glow!=null)glow.SetActive(lit);}
}
