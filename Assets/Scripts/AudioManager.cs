using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField]
    private SoundEffect[] sfxList;
    private Dictionary<string, SoundEffect> sfx;

    [SerializeField]
    private MusicTrack[] musicList;
    private Dictionary<string, MusicTrack> music;

    private int sourcePoolSize = 3;
    private List<AudioSource> sourcePool;
    private AudioSource musicSource;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Add all sound effects from inspector list into dictionary
        sfx = new Dictionary<string, SoundEffect>();
        foreach (SoundEffect sound in sfxList)
            sfx.Add(sound.soundName, sound);
        
        // Add all music tracks from inspector list into dictionary
        music = new Dictionary<string, MusicTrack>();
        foreach (MusicTrack song in musicList)
            music.Add(song.musicName, song);
        
        // Create a pool of audio sources for global sound effects
        sourcePool = new List<AudioSource>();
        for (int i=0; i < sourcePoolSize; i++)
            sourcePool.Add(initializeAudioSource());
        musicSource = initializeAudioSource();

        // Start normal background music
        // playMusic("Music");
    }

    private AudioSource initializeAudioSource()
    {
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        return newSource;
    }

    #nullable enable
    public void playSoundEffect(string soundName)
    {
        if (!sfx.ContainsKey(soundName))
            return;
        SoundEffect sound = sfx[soundName];
        
        #nullable enable
        AudioSource? openSource = getOpenSource();
        if (openSource == null)
            return;
        #nullable disable
        
        openSource.clip = sound.clip;
        openSource.volume = sound.volume;
        openSource.pitch = sound.pitch;
        openSource.loop = sound.loop;
        openSource.Play();
    }

    #nullable enable
    public void playSoundEffect(SoundEffect sound)
    {
        #nullable enable
        AudioSource? openSource = getOpenSource();
        if (openSource == null)
            return;
        #nullable disable
        
        openSource.clip = sound.clip;
        openSource.volume = sound.volume;
        openSource.pitch = sound.pitch;
        openSource.loop = sound.loop;
        openSource.Play();
    }

    public void playMusic(string musicName)
    {
        if (!music.ContainsKey(musicName))
            return;
        MusicTrack song = music[musicName];
        
        stopMusic();
        musicSource.clip = song.clip;
        musicSource.volume = song.volume;
        musicSource.loop = song.loop;
        musicSource.Play();
    }

    public void stopMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Stop();
    }

    // TODO try crossfade?

    #nullable enable
    private AudioSource? getOpenSource()
    {
        foreach (AudioSource source in sourcePool)
        {
            if (!source.isPlaying)
                return source;
        }
        return null;
    }
    #nullable disable

    public void updateLocalizedAudioSource(AudioSource audioSource, string soundName)
    {
        if (!sfx.ContainsKey(soundName))
            return;
        SoundEffect sound = sfx[soundName];
        
        audioSource.clip = sound.clip;
        audioSource.volume = sound.volume;
        audioSource.pitch = sound.pitch;
        audioSource.loop = sound.loop;
    }
}
