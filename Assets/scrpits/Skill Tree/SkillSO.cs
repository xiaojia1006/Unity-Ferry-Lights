using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "NewSkill",
    menuName = "SkillTree/Skill"
)]
public class SkillSO : ScriptableObject
{
    public string skillName;
    public int maxLevel;

    [FormerlySerializedAs("skillTcon")]
    public Sprite skillIcon;
}
