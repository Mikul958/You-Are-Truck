using UnityEngine;

[CreateAssetMenu(fileName = "SoundEffect", menuName = "Audio/SoundEffect")]
public class SoundEffect : ScriptableObject
{
    public string soundName;
    public AudioClip clip;
    public float volume = 1f;
    public float pitch = 1f;
}
