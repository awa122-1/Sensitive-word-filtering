using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace AmongUsFilterMod
{
    public static class CustomBgmCore
    {
        public static readonly List<AudioClip> BgmList = new List<AudioClip>();

        public static int CurrentIndex = 0;
        public static AudioSource BgmAudioSource = null;
        public static bool HasInitialized = false;

        public static float DefaultVolume = 1.0f;
        public static float PracticeVolume = 0.3f;

        // 防止同一首歌结束时被重复切换
        private static bool ChangingTrack = false;

        public static void StopBGM()
        {
            try
            {
                if (BgmAudioSource == null)
                    return;

                BgmAudioSource.Stop();
                BgmAudioSource.clip = null;
                BgmAudioSource.enabled = false;

                ChangingTrack = false;
            }
            catch (Exception ex)
            {
                MyPlugin.Log?.LogError($"[BGM] StopBGM Error: {ex}");
            }
        }

        public static void PlayBGM(float targetVolume)
        {
            try
            {
                if (BgmAudioSource == null)
                    return;

                if (BgmList.Count == 0)
                    return;

                BgmAudioSource.enabled = true;
                BgmAudioSource.volume = targetVolume;

                // 如果已经在播放，就不要重新开始
                if (BgmAudioSource.isPlaying)
                    return;

                // 防止 Index 越界
                if (CurrentIndex < 0 || CurrentIndex >= BgmList.Count)
                    CurrentIndex = 0;

                AudioClip clip = BgmList[CurrentIndex];

                if (clip == null)
                    return;

                BgmAudioSource.clip = clip;

                // 不使用 AudioSource.loop
                BgmAudioSource.loop = false;

                BgmAudioSource.Play();

                ChangingTrack = false;

                MyPlugin.Log?.LogInfo(
                    $"[BGM] Playing: {clip.name} ({CurrentIndex + 1}/{BgmList.Count})"
                );
            }
            catch (Exception ex)
            {
                MyPlugin.Log?.LogError($"[BGM] PlayBGM Error: {ex}");
            }
        }

        public static void NextTrack()
        {
            try
            {
                if (BgmAudioSource == null)
                    return;

                if (BgmList.Count == 0)
                    return;

                if (ChangingTrack)
                    return;

                ChangingTrack = true;

                // 停止当前歌曲
                BgmAudioSource.Stop();

                // 下一首
                CurrentIndex++;

                // 到最后一首以后重新从第一首开始
                if (CurrentIndex >= BgmList.Count)
                    CurrentIndex = 0;

                AudioClip nextClip = BgmList[CurrentIndex];

                if (nextClip == null)
                {
                    ChangingTrack = false;
                    return;
                }

                BgmAudioSource.clip = nextClip;
                BgmAudioSource.loop = false;
                BgmAudioSource.Play();

                MyPlugin.Log?.LogInfo(
                    $"[BGM] Next Track: {nextClip.name} ({CurrentIndex + 1}/{BgmList.Count})"
                );

                ChangingTrack = false;
            }
            catch (Exception ex)
            {
                ChangingTrack = false;

                MyPlugin.Log?.LogError(
                    $"[BGM] NextTrack Error: {ex}"
                );
            }
        }

        public static void CheckBGM()
        {
            try
            {
                if (BgmAudioSource == null)
                    return;

                if (!BgmAudioSource.enabled)
                    return;

                if (BgmList.Count == 0)
                    return;

                AudioClip clip = BgmAudioSource.clip;

                if (clip == null)
                {
                    PlayBGM(DefaultVolume);
                    return;
                }

                // 歌曲播放完毕
                if (!BgmAudioSource.isPlaying)
                {
                    NextTrack();
                    return;
                }

                // 某些 Unity 音频在最后一点时间仍然显示 isPlaying
                if (clip.length > 0f &&
                    BgmAudioSource.time >= clip.length - 0.05f)
                {
                    NextTrack();
                }
            }
            catch
            {
                // 防止 BGM 错误影响游戏
            }
        }
    }


    // =========================================================
    // BGM Update
    // =========================================================

    [HarmonyPatch(typeof(SoundManager), nameof(SoundManager.Update))]
    public static class BgmUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            CustomBgmCore.CheckBGM();
        }
    }


    // =========================================================
    // 初始化
    // =========================================================

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class BgmManagerPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                if (!CustomBgmCore.HasInitialized)
                {
                    CustomBgmCore.HasInitialized = true;

                    LoadMusic();

                    if (CustomBgmCore.BgmList.Count > 0)
                    {
                        GameObject bgmObj =
                            new GameObject("CustomBGMPlayer");

                        UnityEngine.Object.DontDestroyOnLoad(bgmObj);

                        CustomBgmCore.BgmAudioSource =
                            bgmObj.AddComponent<AudioSource>();

                        CustomBgmCore.BgmAudioSource.loop = false;
                        CustomBgmCore.BgmAudioSource.volume =
                            CustomBgmCore.DefaultVolume;

                        CustomBgmCore.BgmAudioSource.spatialBlend = 0f;
                        CustomBgmCore.BgmAudioSource.playOnAwake = false;

                        MyPlugin.Log?.LogInfo(
                            $"[BGM] Loaded {CustomBgmCore.BgmList.Count} songs."
                        );
                    }
                    else
                    {
                        MyPlugin.Log?.LogWarning(
                            "[BGM] No WAV files found."
                        );
                    }
                }

                CustomBgmCore.PlayBGM(
                    CustomBgmCore.DefaultVolume
                );
            }
            catch (Exception ex)
            {
                MyPlugin.Log?.LogError(
                    $"[BGM] Initialization Error: {ex}"
                );
            }
        }


        // =====================================================
        // 加载 Music 文件夹
        // =====================================================

        private static void LoadMusic()
        {
            string gameRootDir =
                Directory.GetCurrentDirectory();

            string musicDir =
                Path.Combine(
                    gameRootDir,
                    "tfumgcgl_data",
                    "Music"
                );

            MyPlugin.Log?.LogInfo(
                $"[BGM] Music directory: {musicDir}"
            );

            if (!Directory.Exists(musicDir))
            {
                MyPlugin.Log?.LogWarning(
                    $"[BGM] Music folder does not exist: {musicDir}"
                );

                return;
            }

            string[] files =
                Directory.GetFiles(
                    musicDir,
                    "*.wav"
                );

            Array.Sort(files);

            foreach (string file in files)
            {
                try
                {
                    AudioClip clip =
                        LoadWavToAudioClip(
                            file,
                            Path.GetFileNameWithoutExtension(file)
                        );

                    if (clip != null)
                    {
                        CustomBgmCore.BgmList.Add(clip);

                        MyPlugin.Log?.LogInfo(
                            $"[BGM] Loaded: {Path.GetFileName(file)}"
                        );
                    }
                    else
                    {
                        MyPlugin.Log?.LogWarning(
                            $"[BGM] Failed to load: {Path.GetFileName(file)}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    MyPlugin.Log?.LogError(
                        $"[BGM] Error loading {file}: {ex}"
                    );
                }
            }
        }


        // =====================================================
        // WAV Loader
        // =====================================================

        private static AudioClip LoadWavToAudioClip(
            string filePath,
            string clipName)
        {
            try
            {
                using (
                    FileStream fs =
                        new FileStream(
                            filePath,
                            FileMode.Open,
                            FileAccess.Read))
                using (
                    BinaryReader reader =
                        new BinaryReader(fs))
                {
                    if (
                        new string(
                            reader.ReadChars(4)
                        ) != "RIFF")
                    {
                        return null;
                    }

                    reader.ReadInt32();

                    if (
                        new string(
                            reader.ReadChars(4)
                        ) != "WAVE")
                    {
                        return null;
                    }

                    int channels = 0;
                    int sampleRate = 0;
                    int bitsPerSample = 0;
                    int audioFormat = 0;

                    byte[] audioData = null;

                    while (fs.Position + 8 <= fs.Length)
                    {
                        string chunkId =
                            new string(
                                reader.ReadChars(4));

                        int chunkSize =
                            reader.ReadInt32();

                        long nextChunk =
                            fs.Position + chunkSize;

                        if (chunkId == "fmt ")
                        {
                            audioFormat =
                                reader.ReadInt16();

                            channels =
                                reader.ReadInt16();

                            sampleRate =
                                reader.ReadInt32();

                            reader.ReadInt32();
                            reader.ReadInt16();

                            bitsPerSample =
                                reader.ReadInt16();
                        }
                        else if (chunkId == "data")
                        {
                            audioData =
                                reader.ReadBytes(chunkSize);

                            break;
                        }

                        fs.Position =
                            Math.Min(
                                nextChunk,
                                fs.Length
                            );
                    }

                    // 目前只支持 PCM
                    if (audioFormat != 1)
                    {
                        MyPlugin.Log?.LogWarning(
                            $"[BGM] Unsupported WAV format: {audioFormat}"
                        );

                        return null;
                    }

                    if (audioData == null)
                        return null;

                    if (channels <= 0)
                        return null;

                    if (sampleRate <= 0)
                        return null;

                    if (
                        bitsPerSample != 8 &&
                        bitsPerSample != 16)
                    {
                        MyPlugin.Log?.LogWarning(
                            $"[BGM] Unsupported bit depth: {bitsPerSample}"
                        );

                        return null;
                    }

                    int bytesPerSample =
                        bitsPerSample / 8;

                    int totalSamples =
                        audioData.Length /
                        bytesPerSample;

                    float[] samples =
                        new float[totalSamples];


                    // 16-bit PCM
                    if (bitsPerSample == 16)
                    {
                        for (int i = 0;
                             i < totalSamples;
                             i++)
                        {
                            short sample =
                                BitConverter.ToInt16(
                                    audioData,
                                    i * 2
                                );

                            samples[i] =
                                sample / 32768f;
                        }
                    }

                    // 8-bit PCM
                    else if (bitsPerSample == 8)
                    {
                        for (int i = 0;
                             i < totalSamples;
                             i++)
                        {
                            samples[i] =
                                (audioData[i] - 128)
                                / 128f;
                        }
                    }

                    int sampleCount =
                        totalSamples / channels;

                    AudioClip clip =
                        AudioClip.Create(
                            clipName,
                            sampleCount,
                            channels,
                            sampleRate,
                            false
                        );

                    clip.SetData(samples, 0);

                    return clip;
                }
            }
            catch (Exception ex)
            {
                MyPlugin.Log?.LogError(
                    $"[BGM] WAV parse error: {ex.Message}"
                );

                return null;
            }
        }
    }


    // =========================================================
    // Lobby
    // =========================================================

    [HarmonyPatch(
        typeof(LobbyBehaviour),
        nameof(LobbyBehaviour.Start))]
    public static class LobbyBgmPlayPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            CustomBgmCore.PlayBGM(
                CustomBgmCore.DefaultVolume
            );
        }
    }


    // =========================================================
    // Game / Practice
    // =========================================================

    [HarmonyPatch(
        typeof(ShipStatus),
        nameof(ShipStatus.Start))]
    public static class InGameOrPracticeBgmPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (TutorialManager.Instance != null)
            {
                CustomBgmCore.PlayBGM(
                    CustomBgmCore.PracticeVolume
                );
            }
            else
            {
                CustomBgmCore.StopBGM();
            }
        }
    }
}