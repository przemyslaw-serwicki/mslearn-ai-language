using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text;

// Import namespaces
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System.Media;


namespace speech_translation
{
    class Program
    {
        private static SpeechConfig speechConfig;
        private static SpeechTranslationConfig translationConfig;

        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string aiSvcKey = configuration["SpeechKey"];
                string aiSvcRegion = configuration["SpeechRegion"];

                // Set console encoding to unicode
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;


                // Configure translation
                translationConfig = SpeechTranslationConfig.FromSubscription(aiSvcKey, aiSvcRegion);
                translationConfig.SpeechRecognitionLanguage = "en-US";
                translationConfig.AddTargetLanguage("fr");
                translationConfig.AddTargetLanguage("es");
                translationConfig.AddTargetLanguage("hi");
                Console.WriteLine("Ready to translate from " + translationConfig.SpeechRecognitionLanguage);


                // Configure speech
                speechConfig = SpeechConfig.FromSubscription(aiSvcKey, aiSvcRegion);


                string targetLanguage = "";
                while (targetLanguage != "quit")
                {
                    Console.WriteLine("\nEnter a target language\n fr = French\n es = Spanish\n hi = Hindi\n Enter anything else to stop\n");
                    targetLanguage=Console.ReadLine().ToLower();
                    if (translationConfig.TargetLanguages.Contains(targetLanguage))
                    {
                        //string translatedText = await TranslateFromMicrophone(targetLanguage);
                        string translatedText = await TranslateFromAudioFile(targetLanguage);
                        //string translatedText = await TranslateFromMicrophoneDirectlyToFile(targetLanguage);
                        //string translatedText = await TranslateFromAudioDirectlyToFile(targetLanguage);
                        Console.WriteLine("Press any key to synthesize the content now by using SpeechConfig!");
                        Console.ReadKey();
                        await SynthesizeTranslation(translatedText, targetLanguage);
                    }
                    else
                    {
                        targetLanguage = "quit";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task<string> TranslateFromMicrophone(string targetLanguage)
        {
            string translation = "";

            // Translate speech
            using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            Console.WriteLine("Speak now...");
            TranslationRecognitionResult result = await translator.RecognizeOnceAsync();
            Console.WriteLine($"Translating '{result.Text}'");
            translation = result.Translations[targetLanguage];
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(translation);
            return translation;
        }

        static async Task<string> TranslateFromAudioFile(string targetLanguage)
        {
            string translation = "";

            // Translate speech
            //string audioFile = "station.wav";
            string audioFile = "gladiator.wav";
            //string audioFile = "dream.wav";
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            //wavPlayer.Play();
            using AudioConfig audioConfig = AudioConfig.FromWavFileInput(audioFile);
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            Console.WriteLine("Getting speech from file...");
            TranslationRecognitionResult result = await translator.RecognizeOnceAsync();
            Console.WriteLine($"Translating '{result.Text}'");
            translation = result.Translations[targetLanguage];
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(translation);
            return translation;
        }

        static async Task SynthesizeTranslation(string translation, string targetLanguage)
        {
            var voices = new Dictionary<string, string>
            {
                ["fr"] = "fr-FR-HenriNeural",
                ["es"] = "es-ES-ElviraNeural",
                ["hi"] = "hi-IN-MadhurNeural"
            };
            speechConfig.SpeechSynthesisVoiceName = voices[targetLanguage];
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);
            SpeechSynthesisResult speak = await speechSynthesizer.SpeakTextAsync(translation);
            if (speak.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine(speak.Reason);
            }
        }

        static async Task<string> TranslateFromMicrophoneDirectlyToFile(string targetLanguage)
        {
            string translation = "";

            // Translate speech
            using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            using SpeechSynthesizer synthesizer = new SpeechSynthesizer(translationConfig);

            Console.WriteLine("Speak now...");
            TranslationRecognitionResult result = await translator.RecognizeOnceAsync();
            Console.WriteLine($"Translating '{result.Text}'");
            translation = result.Translations[targetLanguage];
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(translation);

            // Save the synthesized translation to a file
            var synthesisResult = await synthesizer.SpeakTextAsync(translation);
            using var stream = AudioDataStream.FromResult(synthesisResult);
            string audioFileName = "translation_from_microphone.wav";
            await stream.SaveToWaveFileAsync(audioFileName);
            Console.WriteLine($"Translation saved to {audioFileName}");
            return translation;
        }

        static async Task<string> TranslateFromAudioDirectlyToFile(string targetLanguage)
        {
            string translation = "";

            // Translate speech
            //string audioFile = "station.wav";
            //string audioFile = "gladiator.wav";
            string audioFile = "dream.wav";
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            wavPlayer.Play();
            using AudioConfig audioConfig = AudioConfig.FromWavFileInput(audioFile);
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            Console.WriteLine("Getting speech from file...");
            TranslationRecognitionResult result = await translator.RecognizeOnceAsync();
            Console.WriteLine($"Translating '{result.Text}'");
            translation = result.Translations[targetLanguage];
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(translation);

            // Save the synthesized translation to a file
            Console.WriteLine("Press any key to synthesize the content now by using SpeechTranslationConfig!");
            Console.ReadKey();
            using SpeechSynthesizer synthesizer = new SpeechSynthesizer(translationConfig);
            var synthesisResult = await synthesizer.SpeakTextAsync(translation);
            using var stream = AudioDataStream.FromResult(synthesisResult);
            string audioFileName = "translation_from_audio.wav";
            await stream.SaveToWaveFileAsync(audioFileName);
            Console.WriteLine($"Translation saved to {audioFileName}");
            return translation;
        }
    }
}
