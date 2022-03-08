using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Yomiage.SDK;
using Yomiage.SDK.Common;
using Yomiage.SDK.Config;
using Yomiage.SDK.Talk;
using Yomiage.SDK.VoiceEffects;

namespace CevioAiPlugin
{
    public class VoiceEngine : VoiceEngineBase
    {
        private string exePath => Path.Combine(DllDirectory, "CevioAiSave.exe");

        private string voicesFile => Path.Combine(DllDirectory, "voiceNames.txt");
        private string voiceFile => Path.Combine(DllDirectory, "voiceName.txt");
        private string textFile => Path.Combine(DllDirectory, "text.txt");
        private string wavFile => Path.Combine(DllDirectory, "output.wav");
        private string masterFile => Path.Combine(DllDirectory, "master.txt");

        public override void Initialize(string configDirectory, string dllDirectory, EngineConfig config)
        {
            base.Initialize(configDirectory, dllDirectory, config);

            if (File.Exists(voiceFile)) { File.Delete(voiceFile); }
            if (File.Exists(textFile)) { File.Delete(textFile); }
            if (File.Exists(wavFile)) { File.Delete(wavFile); }
            if (File.Exists(masterFile)) { File.Delete(masterFile); }

            Excute();
        }
        public override async Task<double[]> Play(VoiceConfig mainVoice, VoiceConfig subVoice, TalkScript talkScript, MasterEffectValue masterEffect, Action<int> setSamplingRate_Hz, Action<double[]> submitWavePart)
        {
            await Task.Delay(10);

            if (File.Exists(voiceFile)) { File.Delete(voiceFile); }
            if (File.Exists(textFile)) { File.Delete(textFile); }
            if (File.Exists(wavFile)) { File.Delete(wavFile); }
            if (File.Exists(masterFile)) { File.Delete(masterFile); }

            // 話者設定
            if (mainVoice.Library.Settings.Strings?.TryGetSetting("voiceName", out var voiceNameSetting) == true)
            {
                // ボイスプリセット名で指定
                File.WriteAllText(voiceFile, voiceNameSetting.Value);
            }

            // マスターコントロール の設定
            //File.WriteAllText(masterFile, MasterContorolToJson(masterEffect));
            var effect = new CevioEffect();
            {
                effect.Volume = (int)Math.Round(masterEffect.Volume.GetValueOrDefault(1) * mainVoice.VoiceEffect.Volume.GetValueOrDefault(50) * talkScript.Volume.GetValueOrDefault(1));
                effect.Speed = (int)Math.Round(masterEffect.Speed.GetValueOrDefault(1) * mainVoice.VoiceEffect.Speed.GetValueOrDefault(50) * talkScript.Speed.GetValueOrDefault(1));
                effect.Pitch = (int)Math.Round(masterEffect.Pitch.GetValueOrDefault(1) * mainVoice.VoiceEffect.Pitch.GetValueOrDefault(50) * talkScript.Pitch.GetValueOrDefault(1));
                effect.Emphasis = (int)Math.Round(masterEffect.Emphasis.GetValueOrDefault(1) * mainVoice.VoiceEffect.Emphasis.GetValueOrDefault(50) * talkScript.Emphasis.GetValueOrDefault(1));
                effect.Alpha = (int)Math.Round(mainVoice.VoiceEffect.AdditionalEffect.GetValueOrDefault("Alpha").GetValueOrDefault(50) * talkScript.AdditionalEffect.GetValueOrDefault("Alpha").GetValueOrDefault(1));
                effect.Emotion1 = (int)Math.Round(mainVoice.VoiceEffect.AdditionalEffect.GetValueOrDefault("Emotion1").GetValueOrDefault(0) + talkScript.AdditionalEffect.GetValueOrDefault("Emotion1").GetValueOrDefault(0));
                effect.Emotion2 = (int)Math.Round(mainVoice.VoiceEffect.AdditionalEffect.GetValueOrDefault("Emotion2").GetValueOrDefault(0) + talkScript.AdditionalEffect.GetValueOrDefault("Emotion2").GetValueOrDefault(0));
                effect.Emotion3 = (int)Math.Round(mainVoice.VoiceEffect.AdditionalEffect.GetValueOrDefault("Emotion3").GetValueOrDefault(0) + talkScript.AdditionalEffect.GetValueOrDefault("Emotion3").GetValueOrDefault(0));
                effect.Emotion4 = (int)Math.Round(mainVoice.VoiceEffect.AdditionalEffect.GetValueOrDefault("Emotion4").GetValueOrDefault(0) + talkScript.AdditionalEffect.GetValueOrDefault("Emotion4").GetValueOrDefault(0));
            }

            var xmlSerializer1 = new XmlSerializer(typeof(CevioEffect));
            using (var streamWriter = new StreamWriter(masterFile, false, Encoding.UTF8))
            {
                xmlSerializer1.Serialize(streamWriter, effect);
                streamWriter.Flush();
            }


            // テキスト設定
            File.WriteAllText(textFile, talkScript.OriginalText.Replace(Environment.NewLine, String.Empty));

            Excute();

            if (File.Exists(wavFile))
            {
                using var reader = new WaveFileReader(wavFile);
                int fs = reader.WaveFormat.SampleRate;
                setSamplingRate_Hz(fs);

                // var wave = new List<double>();
                var wave = new List<double>(talkScript.Sections.First().Pause.Span_ms * fs / 1000); // フレーズ編集の最初のポーズは効く
                while (reader.Position < reader.Length)
                {
                    var samples = reader.ReadNextSampleFrame();
                    wave.Add(samples.First());
                }
                wave.AddRange(new double[talkScript.EndSection.Pause.Span_ms * fs / 1000]); // フレーズ編集の最後のポーズは効く
                wave.AddRange(new double[((int)masterEffect.EndPause) * fs / 1000]); // 文末ポーズ
                return wave.ToArray();
            }

            return new double[0];
        }


        private void Excute()
        {
            if (!File.Exists(exePath))
            {
                return;
            }

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = exePath,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = DllDirectory,
            };
            var process = Process.Start(processStartInfo);
            process.WaitForExit(3000);
            process.Kill();
            process.Dispose();
        }


    }
}
