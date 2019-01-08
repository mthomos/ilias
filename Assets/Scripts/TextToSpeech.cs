// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using UnityEngine;

#if !UNITY_EDITOR && UNITY_WSA
using Windows.Foundation;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using System.Linq;
using System.Threading.Tasks;
#endif

[RequireComponent(typeof(AudioSource))]

public class TextToSpeech : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioSource AudioSource { get { return audioSource; } set { audioSource = value; } }

    public static TextToSpeech Instance { get; private set; }
#if !UNITY_EDITOR && UNITY_WSA
    private SpeechSynthesizer synthesizer;
    private VoiceInformation voiceInfo;
    private bool speechTextInQueue = false;
#endif


    private static float BytesToFloat(byte firstByte, byte secondByte)
    {
        // Convert two bytes to one short (little endian)
        short s = (short)((secondByte << 8) | firstByte);

        // Convert to range from -1 to (just below) 1
        return s / 32768.0F;
    }

    private static int BytesToInt(byte[] bytes, int offset = 0)
    {
        int value = 0;
        for (int i = 0; i < 4; i++)
        {
            value |= ((int)bytes[offset + i]) << (i * 8);
        }
        return value;
    }

    /// <summary>
    /// Dynamically creates an <see cref="AudioClip"/> that represents raw Unity audio data.
    /// </summary>
    /// <param name="name"> The name of the dynamically generated clip.</param>
    /// <param name="audioData">Raw Unity audio data.</param>
    /// <param name="sampleCount">The number of samples in the audio data.</param>
    /// <param name="frequency">The frequency of the audio data.</param>
    /// <returns>The <see cref="AudioClip"/>.</returns>
    private static AudioClip ToClip(string name, float[] audioData, int sampleCount, int frequency)
    {
        var clip = AudioClip.Create(name, sampleCount, 1, frequency, false);
        clip.SetData(audioData, 0);
        return clip;
    }

    private static float[] ToUnityAudio(byte[] wavAudio, out int sampleCount, out int frequency)
    {
        // Determine if mono or stereo
        int channelCount = wavAudio[22];  // Speech audio data is always mono but read actual header value for processing

        // Get the frequency
        frequency = BytesToInt(wavAudio, 24);

        // Get past all the other sub chunks to get to the data subchunk:
        int pos = 12; // First subchunk ID from 12 to 16

        // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
        while (!(wavAudio[pos] == 100 && wavAudio[pos + 1] == 97 && wavAudio[pos + 2] == 116 && wavAudio[pos + 3] == 97))
        {
            pos += 4;
            int chunkSize = wavAudio[pos] + wavAudio[pos + 1] * 256 + wavAudio[pos + 2] * 65536 + wavAudio[pos + 3] * 16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;

        // Pos is now positioned to start of actual sound data.
        sampleCount = (wavAudio.Length - pos) / 2;  // 2 bytes per sample (16 bit sound mono)
        if (channelCount == 2) { sampleCount /= 2; }  // 4 bytes per sample (16 bit stereo)

        // Allocate memory (supporting left channel only)
        var unityData = new float[sampleCount];

        // Write to double array/s:
        int i = 0;
        while (pos < wavAudio.Length)
        {
            unityData[i] = BytesToFloat(wavAudio[pos], wavAudio[pos + 1]);
            pos += 2;
            if (channelCount == 2)
                pos += 2;

            i++;
        }
        return unityData;
    }

#if !UNITY_EDITOR && UNITY_WSA
    /// <summary>
    /// Executes a function that generates a speech stream and then converts and plays it in Unity.
    /// </summary>
    /// <param name="text">
    /// A raw text version of what's being spoken for use in debug messages when speech isn't supported.
    /// </param>
    /// <param name="speakFunc">
    /// The actual function that will be executed to generate speech.
    /// </param>
    private void PlaySpeech(string text, Func<IAsyncOperation<SpeechSynthesisStream>> speakFunc)
    {
        // Make sure there's something to speak
        if (speakFunc == null) throw new ArgumentNullException(nameof(speakFunc));

        if (synthesizer != null)
        {
            try
            {
                speechTextInQueue = true;
                // Need await, so most of this will be run as a new Task in its own thread.
                // This is good since it frees up Unity to keep running anyway.
                Task.Run(async () =>
                {
                    // Speak and get stream
                    var speechStream = await speakFunc();

                    // Get the size of the original stream
                    var size = speechStream.Size;

                    // Create buffer
                    byte[] buffer = new byte[(int)size];

                    // Get input stream and the size of the original stream
                    using (var inputStream = speechStream.GetInputStreamAt(0))
                    {
                        // Close the original speech stream to free up memory
                        speechStream.Dispose();

                        // Create a new data reader off the input stream
                        using (var dataReader = new DataReader(inputStream))
                        {
                            // Load all bytes into the reader
                            await dataReader.LoadAsync((uint)size);

                            // Copy from reader into buffer
                            dataReader.ReadBytes(buffer);
                        }
                    }

                    // Convert raw WAV data into Unity audio data
                    int sampleCount = 0;
                    int frequency = 0;
                    var unityData = ToUnityAudio(buffer, out sampleCount, out frequency);

                    // The remainder must be done back on Unity's main thread
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        // Convert to an audio clip
                        var clip = ToClip("Speech", unityData, sampleCount, frequency);

                        // Set the source on the audio clip
                        audioSource.clip = clip;

                        // Play audio
                        audioSource.Play();
                        speechTextInQueue = false;
                    }, false);
                });
            }
            catch (Exception ex)
            {
                speechTextInQueue = false;
                Debug.LogErrorFormat("Speech generation problem: \"{0}\"", ex.Message);
            }
        }
        else
        {
            Debug.LogErrorFormat("Speech not initialized. \"{0}\"", text);
        }
    }
#endif

    private void Awake()
    {
        try
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
#if !UNITY_EDITOR && UNITY_WSA
            synthesizer = new SpeechSynthesizer();
#endif
            Instance = this;
        }
        catch (Exception ex)
        {
            Debug.LogError("Could not start Speech Synthesis: " + ex.Message);
        }
    }

    // Public Methods

    /// <summary>
    /// Speaks the specified SSML markup using text-to-speech.
    /// </summary>
    /// <param name="ssml">The SSML markup to speak.</param>
    public void SpeakSsml(string ssml)
    {
        // Make sure there's something to speak
        if (string.IsNullOrEmpty(ssml)) { return; }

        // Pass to helper method
#if !UNITY_EDITOR && UNITY_WSA
        PlaySpeech(ssml, () => synthesizer.SynthesizeSsmlToStreamAsync(ssml));
#else
        Debug.LogWarningFormat("Text to Speech not supported in editor.\n\"{0}\"", ssml);
#endif
    }

    /// <summary>
    /// Speaks the specified text using text-to-speech.
    /// </summary>
    /// <param name="text">The text to speak.</param>
    public void StartSpeaking(string text)
    {
        // Make sure there's something to speak
        if (string.IsNullOrEmpty(text)) { return; }

        // Pass to helper method
#if !UNITY_EDITOR && UNITY_WSA
        PlaySpeech(text, ()=> synthesizer.SynthesizeTextToStreamAsync(text));
#else
        Debug.LogWarningFormat("Text to Speech not supported in editor.\n\"{0}\"", text);
#endif
    }

    public bool SpeechTextInQueue()
    {
#if !UNITY_EDITOR && UNITY_WSA
        return speechTextInQueue;
#else
        return false;
#endif
    }

    public bool IsSpeaking()
    {
        if (audioSource != null)
            return audioSource.isPlaying;

        return false;
    }

    public void StopSpeaking()
    {
        if (IsSpeaking())
            audioSource.Stop();
    }
}