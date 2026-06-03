using UnityEngine;

public class SetGameFlag : MonoBehaviour
{
    [SerializeField] private string flagTag;

    public void SetFlag() => GameEventManager.Set(flagTag);
}
