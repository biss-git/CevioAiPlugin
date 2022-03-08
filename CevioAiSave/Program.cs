using CeVIO.Talk.RemoteService2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CevioAiSave
{
    internal class Program
    {
        private static readonly string voicesFile = "voiceNames.txt";
        private static readonly string voiceFile = "voiceName.txt";
        private static readonly string textFile = "text.txt";
        private static readonly string wavFile = "output.wav";
        private static readonly string masterFile = "master.txt";

        static void Main(string[] args)
        {
            // 【CeVIO AI】起動
            ServiceControl2.StartHost(false);
            var talker = new Talker2();
            var text = string.Empty;

            {
                // 見つけたプリセット名を列挙する
                var voiceNames = string.Empty;
                try
                {
                    foreach (var voice in Talker2.AvailableCasts)
                    {
                        voiceNames += voice + Environment.NewLine;
                    }
                }
                catch (Exception)
                {
                    return;
                }
                File.WriteAllText(voicesFile, voiceNames);
            }

            {
                // 音声の指定を受け取る
                var voiceName = Talker2.AvailableCasts.First();
                if (File.Exists(voiceFile))
                {
                    voiceName = File.ReadAllLines(voiceFile).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                }
                if (!Talker2.AvailableCasts.Contains(voiceName))
                {
                    voiceName = Talker2.AvailableCasts.First();
                }

                talker.Cast = voiceName;
            }

            {
                // 音声効果を設定する
                if (File.Exists(masterFile))
                {
                    var xmlSerializer2 = new XmlSerializer(typeof(CevioEffect));
                    var effect = new CevioEffect();
                    CevioEffect result;
                    var xmlSettings = new System.Xml.XmlReaderSettings()
                    {
                        CheckCharacters = false, // （2）
                    };
                    using (var streamReader = new StreamReader(masterFile, Encoding.UTF8))
                    using (var xmlReader
                            = System.Xml.XmlReader.Create(streamReader, xmlSettings))
                    {
                        result = (CevioEffect)xmlSerializer2.Deserialize(xmlReader); // （3）
                    }

                    effect = result;

                    talker.Volume = (uint)Math.Min( Math.Max(0, effect.Volume), 100);
                    talker.Speed = (uint)Math.Min(Math.Max(0, effect.Speed), 100);
                    talker.Tone = (uint)Math.Min(Math.Max(0, effect.Pitch), 100);
                    talker.Alpha = (uint)Math.Min(Math.Max(0, effect.Alpha), 100);
                    talker.ToneScale = (uint)Math.Min(Math.Max(0, effect.Emphasis), 100);
                    var emotions = talker.Components.ToList();
                    if (emotions.Count > 0) { emotions[0].Value = (uint)Math.Min(Math.Max(0, effect.Emotion1), 100); }
                    if (emotions.Count > 1) { emotions[1].Value = (uint)Math.Min(Math.Max(0, effect.Emotion2), 100); }
                    if (emotions.Count > 2) { emotions[2].Value = (uint)Math.Min(Math.Max(0, effect.Emotion3), 100); }
                    if (emotions.Count > 3) { emotions[3].Value = (uint)Math.Min(Math.Max(0, effect.Emotion4), 100); }
                }
            }

            {
                // 読み上げるテキストを受け取る
                if (File.Exists(textFile))
                {
                    text = File.ReadAllText(textFile);
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }
            }

            {
                // 音声を保存する。
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                path = Path.Combine(path, wavFile);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                talker.OutputWaveToFile(text, path);
            }
        }
    }
}
