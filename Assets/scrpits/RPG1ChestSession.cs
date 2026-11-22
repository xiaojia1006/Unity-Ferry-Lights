using System.Collections.Generic;
using UnityEngine;

// Scene re-entry cannot reopen a chest already claimed during this play session.
[DefaultExecutionOrder(100)]
public sealed class RPG1ChestSession : MonoBehaviour
{
    public string chestId;
    private TreasureChest chest;
    private static readonly HashSet<string> opened=new HashSet<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession(){opened.Clear();}
    private void Start(){chest=GetComponent<TreasureChest>();if(opened.Contains(chestId)){chest.isOpen=true;var animator=GetComponent<Animator>();if(animator!=null)animator.Play("Chest_Open",0,1);}}
    private void Update(){if(chest!=null&&chest.isOpen)opened.Add(chestId);}
}
