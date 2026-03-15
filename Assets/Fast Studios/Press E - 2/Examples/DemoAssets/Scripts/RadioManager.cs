using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.ComponentModel;

namespace FastStudios.Demo
{
    public class RadioManager : MonoBehaviour
    {
        public Interactable FrequencySlider;
        public Interactable VolumeKnob;
        public RotationLerp OnOffAntena;

        public bool TurnOn => !OnOffAntena.IsInOriginalState();

        [Header("Audios")]
        public AudioSource RadioSource;
        public AudioClip StaticSound;
        private AudioSource staticSource;
        private AudioSource crossSource;

        [Header("Frequency")]
        [ReadOnly(true)] public float actualFrequency = 88f;
        public float tuningWidth = 2f;
        [Range(0.5f, 8f)] public float tuningSharpness = 2f;

        [Header("Mix / Volumes")]
        [Range(0, 1)] public float maxVol = 1f;
        [Range(0.1f, 0.95f)] public float mixHeadroom = 0.8f;
        [Range(0, 1)] public float staticBaseStrength = 0.25f;
        [Range(0.01f, 0.5f)] public float volumeSmoothTime = 0.08f;

        [Header("Signal Detection")]
        public float rmsTarget = 0f;
        [Range(0, 1)] public float lowSignalStaticStrength = 0.5f;
        public float updateStep = 0.05f;

        [Header("UI")]
        public TMP_Text FrequencyDisplayText;
        public int DecimalHouses = 1;

        [Header("Stations")]
        public List<FrequencyClips> FrequencyClips = new();

        [Header("Continuos Broadcast")]
        public bool continuousBroadcast = true;
        [Range(0f, 30f)] public float periodicResyncSeconds = 10f;

        [Header("Performance / Preload")]
        public bool preloadAllClipsOnAwake = true;
        [Range(1, 8)] public int clipsPerFrame = 2;

        private float[] sampleBuffer;
        private float nextMeasureAt;
        private float lastRms;
        private float vPrimVel, vCrossVel, vStaticVel;
        private string initialText;
        private int primIndex = -1, crossIndex = -1;

        private double broadcastClock;
        private double nextPeriodicResyncAt;
        private readonly Dictionary<AudioClip, float> stationOffsetSec = new();

        private readonly Dictionary<AudioSource, AudioClip> pendingSet = new();

        private void Awake()
        {
            initialText = FrequencyDisplayText ? FrequencyDisplayText.text : null;

            actualFrequency = FrequencySlider.GetSliderValue();
            maxVol = 1 - VolumeKnob.GetSliderValue();

            staticSource = RadioSource.CopyComponent(RadioSource.gameObject);
            staticSource.clip = StaticSound;
            staticSource.loop = true;

            crossSource = RadioSource.CopyComponent(RadioSource.gameObject);
            crossSource.clip = null;
            crossSource.loop = true;

            if (RadioSource) RadioSource.volume = 0f;
            if (staticSource) staticSource.volume = 0f;
            if (crossSource) crossSource.volume = 0f;

            sampleBuffer = new float[1024];
            nextMeasureAt = 0f;

            EnsureStationsSortedAndSpaced();
            InitStationOffsets();

            broadcastClock = 0.0;
            nextPeriodicResyncAt = periodicResyncSeconds > 0 ? periodicResyncSeconds : double.PositiveInfinity;

            if (preloadAllClipsOnAwake)
                StartCoroutine(PrewarmClipsCoroutine());
        }

        private void OnValidate()
        {
            mixHeadroom = Mathf.Clamp01(mixHeadroom);
            clipsPerFrame = Mathf.Clamp(clipsPerFrame, 1, 8);
        }

        private void Update()
        {
            broadcastClock += Time.unscaledDeltaTime;

            if (!TurnOn)
            {
                if (staticSource.isPlaying && staticSource.clip) staticSource.Stop();
                if (crossSource.clip && crossSource.isPlaying) crossSource.Stop();
                if (RadioSource.clip && RadioSource.isPlaying) RadioSource.Stop();
                return;
            }
            
            actualFrequency = FrequencySlider.GetSliderValue();
            maxVol = 1 - VolumeKnob.GetSliderValue();

            if (FrequencyDisplayText)
            {
                string beforeUpdatedText = initialText;

                var show = actualFrequency.ToString($"F{DecimalHouses}") + " Hz";
                if (!string.IsNullOrEmpty(initialText) && initialText.Contains("{freq}"))
                {
                    FrequencyDisplayText.text = initialText.Replace("{freq}", show);
                    beforeUpdatedText = FrequencyDisplayText.text;
                }

                var vol = (maxVol * 100).ToString("F0") + "%";
                if (!string.IsNullOrEmpty(beforeUpdatedText) && beforeUpdatedText.Contains("{vol}"))
                {
                    FrequencyDisplayText.text = beforeUpdatedText.Replace("{vol}", vol);
                }
            }

            if (!RadioSource || !staticSource) return;

            if (FrequencyClips == null || FrequencyClips.Count == 0)
            {
                SmoothVolumes(0f, 0f, maxVol);
                return;
            }

            (int a, int b) = FindTwoNearestIndices(actualFrequency);

            FrequencyClips stA = a >= 0 ? FrequencyClips[a] : null;
            FrequencyClips stB = b >= 0 ? FrequencyClips[b] : null;

            if (stA != null && a != primIndex)
            {
                if (continuousBroadcast) TrySetClipWithBroadcastLazy(RadioSource, stA.clip);
                else SetClipNormal(RadioSource, stA.clip);
                primIndex = a;
            }
            if (stB != null && b != crossIndex)
            {
                if (continuousBroadcast) TrySetClipWithBroadcastLazy(crossSource, stB.clip);
                else SetClipNormal(crossSource, stB.clip);
                crossIndex = b;
            }

            ProcessPendingSets();

            if (continuousBroadcast && periodicResyncSeconds > 0f && Time.unscaledTimeAsDouble >= nextPeriodicResyncAt)
            {
                if (RadioSource && RadioSource.clip) HardResyncToBroadcast(RadioSource);
                if (crossSource && crossSource.clip) HardResyncToBroadcast(crossSource);
                nextPeriodicResyncAt = Time.unscaledTimeAsDouble + periodicResyncSeconds;
            }

            float wA = stA != null ? TuningWeight(Mathf.Abs(actualFrequency - stA.Frequency)) : 0f;
            float wB = stB != null ? TuningWeight(Mathf.Abs(actualFrequency - stB.Frequency)) : 0f;

            float stationsHeadroom = mixHeadroom * maxVol;
            float sumW = wA + wB;
            float vA = 0f, vB = 0f;
            if (sumW > 0f)
            {
                float k = stationsHeadroom / sumW;
                vA = wA * k;
                vB = wB * k;
            }

            if (Time.unscaledTime >= nextMeasureAt)
            {
                float rA = MeasureRms(RadioSource);
                float rB = MeasureRms(crossSource);
                lastRms = Mathf.Max(rA, rB);
                nextMeasureAt = Time.unscaledTime + Mathf.Max(0.01f, updateStep);
            }
            float amp01 = Mathf.Clamp01(rmsTarget > 0f ? (lastRms / rmsTarget) : 1f);

            float bestW = Mathf.Max(wA, wB);
            float staticBase = (1f - bestW) * staticBaseStrength;
            float staticBonus = (1f - amp01) * bestW * lowSignalStaticStrength;

            float staticHeadroom = (1f - mixHeadroom) * maxVol;
            float vStatic = Mathf.Clamp01(staticBase + staticBonus) * staticHeadroom;

            SmoothVolumes(vA, vB, vStatic);

            if (staticSource.clip != StaticSound) staticSource.clip = StaticSound;
            if (!staticSource.isPlaying && staticSource.clip) staticSource.Play();
            if (crossSource.clip && !crossSource.isPlaying) crossSource.Play();
            if (RadioSource.clip && !RadioSource.isPlaying) RadioSource.Play();
        }

        private void SmoothVolumes(float vA, float vB, float vStatic)
        {
            if (RadioSource) RadioSource.volume = Mathf.SmoothDamp(RadioSource.volume, vA, ref vPrimVel, volumeSmoothTime);
            if (crossSource) crossSource.volume = Mathf.SmoothDamp(crossSource.volume, vB, ref vCrossVel, volumeSmoothTime);
            if (staticSource) staticSource.volume = Mathf.SmoothDamp(staticSource.volume, vStatic, ref vStaticVel, volumeSmoothTime);
        }

        private float TuningWeight(float delta)
        {
            float t = Mathf.InverseLerp(tuningWidth, 0f, delta);
            return (tuningSharpness == 1f) ? t : Mathf.Pow(Mathf.Clamp01(t), tuningSharpness);
        }

        private (int a, int b) FindTwoNearestIndices(float f)
        {
            if (FrequencyClips == null || FrequencyClips.Count == 0) return (-1, -1);
            if (FrequencyClips.Count == 1) return (0, -1);

            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < FrequencyClips.Count; i++)
            {
                float d = Mathf.Abs(FrequencyClips[i].Frequency - f);
                if (d < best) { best = d; nearest = i; }
            }

            int second = -1;
            float best2 = float.MaxValue;
            for (int i = 0; i < FrequencyClips.Count; i++)
            {
                if (i == nearest) continue;
                float d = Mathf.Abs(FrequencyClips[i].Frequency - f);
                if (d < best2) { best2 = d; second = i; }
            }

            if (second >= 0 && FrequencyClips[second].Frequency < FrequencyClips[nearest].Frequency)
                return (second, nearest);
            return (nearest, second);
        }

        private void InitStationOffsets()
        {
            stationOffsetSec.Clear();
            if (FrequencyClips == null) return;
            foreach (var fc in FrequencyClips)
            {
                if (fc?.clip == null) continue;
                stationOffsetSec[fc.clip] = 0f;
            }
        }

        private float GetStationOffsetSec(AudioClip clip)
        {
            if (clip == null) return 0f;
            if (!stationOffsetSec.TryGetValue(clip, out float off))
            {
                off = 0f;
                stationOffsetSec[clip] = off;
            }
            return off;
        }

        private void SetClipNormal(AudioSource src, AudioClip clip)
        {
            if (!src) return;
            src.clip = clip;
            src.loop = true;
            if (clip != null) src.Play();
        }

        private void TrySetClipWithBroadcastLazy(AudioSource src, AudioClip clip)
        {
            if (!src) return;
            if (clip == null)
            {
                src.clip = null;
                pendingSet.Remove(src);
                return;
            }

            var state = clip.loadState;
            if (state == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            if (state == AudioDataLoadState.Loaded)
            {
                SetClipWithBroadcastTime(src, clip);
                pendingSet.Remove(src);
            }
            else
            {
                pendingSet[src] = clip;

                src.clip = null;
            }
        }

        private void ProcessPendingSets()
        {
            if (pendingSet.Count == 0) return;

            var keys = ListPool<AudioSource>.Get();
            keys.AddRange(pendingSet.Keys);

            foreach (var src in keys)
            {
                var clip = pendingSet[src];
                if (clip == null) { pendingSet.Remove(src); continue; }

                var state = clip.loadState;
                if (state == AudioDataLoadState.Loaded)
                {
                    SetClipWithBroadcastTime(src, clip);
                    pendingSet.Remove(src);
                }
                else if (state == AudioDataLoadState.Failed)
                {
                    SetClipNormal(src, clip);
                    pendingSet.Remove(src);
                }
            }

            ListPool<AudioSource>.Release(keys);
        }

        private void SetClipWithBroadcastTime(AudioSource src, AudioClip clip)
        {
            src.clip = clip;
            src.loop = true;
            if (clip == null) return;

            float len = Mathf.Max(0.001f, clip.length);
            double posSec = (GetStationOffsetSec(clip) + broadcastClock) % len;

            int targetSamples = Mathf.FloorToInt((float)posSec * clip.frequency);
            targetSamples = Mathf.Clamp(targetSamples, 0, Mathf.Max(0, clip.samples - 1));

            src.timeSamples = targetSamples;
            src.Play();
        }

        private void HardResyncToBroadcast(AudioSource src)
        {
            if (!src || src.clip == null) return;
            float len = Mathf.Max(0.001f, src.clip.length);
            double posSec = (GetStationOffsetSec(src.clip) + broadcastClock) % len;
            int targetSamples = Mathf.FloorToInt((float)posSec * src.clip.frequency);
            targetSamples = Mathf.Clamp(targetSamples, 0, Mathf.Max(0, src.clip.samples - 1));
            src.timeSamples = targetSamples;
        }

        private float MeasureRms(AudioSource src)
        {
            if (!src || !src.isPlaying || src.clip == null) return 0f;
            try
            {
                if (sampleBuffer == null || sampleBuffer.Length == 0) sampleBuffer = new float[1024];
                src.GetOutputData(sampleBuffer, 0);
                double sum = 0.0;
                for (int i = 0; i < sampleBuffer.Length; i++)
                    sum += sampleBuffer[i] * sampleBuffer[i];
                return Mathf.Sqrt((float)(sum / sampleBuffer.Length));
            }
            catch { return 0f; }
        }

        private void EnsureStationsSortedAndSpaced()
        {
            if (FrequencyClips == null) return;
            FrequencyClips.Sort((x, y) => x.Frequency.CompareTo(y.Frequency));

            for (int i = 0; i < FrequencyClips.Count - 1; i++)
            {
                float d = Mathf.Abs(FrequencyClips[i + 1].Frequency - FrequencyClips[i].Frequency);
                if (d < tuningWidth)
                {
                    Debug.LogWarning($"[RadioManager] Estações com distância < tuningWidth: " +
                                     $"{FrequencyClips[i].Frequency} e {FrequencyClips[i + 1].Frequency} (Δ={d:F3} < {tuningWidth}).");
                }
            }
        }

        private IEnumerator PrewarmClipsCoroutine()
        {
            var unique = ListPool<AudioClip>.Get();
            if (StaticSound) unique.Add(StaticSound);
            if (FrequencyClips != null)
            {
                for (int i = 0; i < FrequencyClips.Count; i++)
                {
                    var c = FrequencyClips[i]?.clip;
                    if (c != null && !unique.Contains(c)) unique.Add(c);
                }
            }

            int processed = 0;
            for (int i = 0; i < unique.Count; i++)
            {
                var clip = unique[i];
                if (clip == null) continue;

                if (clip.loadState == AudioDataLoadState.Unloaded)
                    clip.LoadAudioData();

                while (clip.loadState == AudioDataLoadState.Loading)
                    yield return null;

                processed++;
                if (processed % Mathf.Max(1, clipsPerFrame) == 0)
                    yield return null;
            }

            ListPool<AudioClip>.Release(unique);
        }
    }

    [System.Serializable]
    public class FrequencyClips
    {
        public float Frequency;
        public AudioClip clip;
    }

    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new();
        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(8);
        public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
    }
}
