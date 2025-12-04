using UnityEngine;

[CreateAssetMenu(fileName = "MusicTrack", menuName = "Audio/MusicTrack")]
public class MusicTrack : ScriptableObject
{
    public string musicName;
    public AudioClip clip;
    public float volume = 1f;
    public bool loop = true;
}
