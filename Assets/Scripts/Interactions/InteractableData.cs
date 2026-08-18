using UnityEngine;

[CreateAssetMenu(
    fileName = "NewInteractableData", menuName = "Data/Interactable")]
public class InteractableData : ScriptableObject
{
    public AnimationClip clip;
}
