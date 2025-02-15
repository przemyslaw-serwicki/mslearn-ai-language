﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text;

// Import namespaces
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System.Media;
using System.IO;


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
                        Console.WriteLine("Enter your choice (1-4):");
                        Console.WriteLine("1. Translate from microphone");
                        Console.WriteLine("2. Translate from audio file");
                        Console.WriteLine("3. Translate from microphone directly to file");
                        Console.WriteLine("4. Translate from audio directly to file");
                        Console.WriteLine("5. Translate from microphone with event based synthesis - speech-to-speech");
                        Console.WriteLine("6. Translate from audio with event based synthesis - speech-to-speech");

                        string choice = Console.ReadLine();

                        string translatedText = ""; // Initialize

                        switch (choice)
                        {
                            case "1":
                                translatedText = await TranslateFromMicrophone(targetLanguage);
                                break;
                            case "2":
                                translatedText = await TranslateFromAudioFile(targetLanguage);
                                break;
                            case "3":
                                translatedText = await TranslateFromMicrophoneDirectlyToFile(targetLanguage);
                                break;
                            case "4":
                                translatedText = await TranslateFromAudioDirectlyToFile(targetLanguage);
                                break;
                            case "5":
                                await TranslateFromMicrophoneWithEventBasedSynthesis(targetLanguage);
                                break;
                            case "6":
                                await TranslateFromAudioFileWithEventBasedSynthesis(targetLanguage);
                                break;
                            default:
                                Console.WriteLine("Invalid choice");
                                break;
                        }

                        Console.WriteLine("Synthesize the translation? (Y/N)");
                        string synthesizeChoice = Console.ReadLine().ToLower();

                        if (synthesizeChoice == "y")
                        {
                            await SynthesizeTranslation(translatedText, targetLanguage);
                        }
                        else
                        {
                            Console.WriteLine("Skipping synthesis.");
                        }
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
            Console.WriteLine("Enter a 's'->(station), 'g'->(goodman) or 'h'->(house) to choose audio file. By default I would use station file");
            string flow = Console.ReadLine();

            string audioFile = flow.Trim() switch
            {
                "g" => "goodman.wav",
                "h" => "house.wav",
                _ => "station.wav",
            };
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
            return translation;
        }

        static async Task<string> TranslateFromMicrophoneDirectlyToFile(string targetLanguage)
        {
            string translation = "";

            // Translate speech
            using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(translationConfig);

            Console.WriteLine("Speak now...");
            TranslationRecognitionResult result = await translator.RecognizeOnceAsync();
            Console.WriteLine($"Translating '{result.Text}'");
            translation = result.Translations[targetLanguage];
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(translation);

            // Save the synthesized translation to a file
            var synthesisResult = await speechSynthesizer.SpeakTextAsync(translation);
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
            Console.WriteLine("Enter a 's'->(station), 'g'->(goodman) or 'h'->(house) to choose audio file. By default I would use station file");
            string flow = Console.ReadLine();
            string audioFile = flow.Trim() switch
            {
                "g" => "goodman.wav",
                "h" => "house.wav",
                _ => "station.wav",
            };
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            wavPlayer.Play();
            
            using AudioConfig audioConfig = AudioConfig.FromWavFileInput(audioFile);
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(translationConfig);

            Console.WriteLine("Getting speech from file...");
            TranslationRecognitionResult result = await translator.RecognizeOnceAsync();
            Console.WriteLine($"Translating '{result.Text}'");
            translation = result.Translations[targetLanguage];
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(translation);

            // Save the synthesized translation to a file
            Console.WriteLine("Press any key to synthesize the content now by using SpeechTranslationConfig!");
            Console.ReadKey();
            var synthesisResult = await speechSynthesizer.SpeakTextAsync(translation);
            using var stream = AudioDataStream.FromResult(synthesisResult);
            string audioFileName = "translation_from_audio.wav";
            await stream.SaveToWaveFileAsync(audioFileName);
            Console.WriteLine($"Translation saved to {audioFileName}");
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

        static async Task TranslateFromMicrophoneWithEventBasedSynthesis(string targetLanguage)
        {
            translationConfig.SpeechRecognitionLanguage = "en-US";
            translationConfig.RemoveTargetLanguage("fr");
            translationConfig.RemoveTargetLanguage("es");
            translationConfig.RemoveTargetLanguage("hi");
            translationConfig.AddTargetLanguage(targetLanguage);
            using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);
            Console.WriteLine("Speak now...");

            //IT DOES NOT WORK
            translator.Synthesizing += (_, e) =>
            {
                var audio = e.Result.GetAudio();
                Console.WriteLine($"Audio synthesized: {audio.Length:#,0} byte(s) {(audio.Length == 0 ? "(Complete)" : "")}");

                if (audio.Length > 0)
                {
                    File.WriteAllBytes("EventBasedSynthesisFromMicrophone.wav", audio);
                }
            };

            var result = await translator.RecognizeOnceAsync();
            if (result.Reason == ResultReason.TranslatedSpeech)
            {
                Console.WriteLine($"Recognized: \"{result.Text}\"");
            }
        }

        static async Task TranslateFromAudioFileWithEventBasedSynthesis(string targetLanguage)
        {
            // Translate speech
            Console.WriteLine("Enter a 's'->(station), 'g'->(goodman) or 'h'->(house) to choose audio file. By default I would use station file");
            string flow = Console.ReadLine();

            string audioFile = flow.Trim() switch
            {
                "g" => "goodman.wav",
                "h" => "house.wav",
                _ => "station.wav",
            };
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            wavPlayer.Play();

            translationConfig.SpeechRecognitionLanguage = "en-US";
            translationConfig.RemoveTargetLanguage("fr");
            translationConfig.RemoveTargetLanguage("es");
            translationConfig.RemoveTargetLanguage("hi");
            translationConfig.AddTargetLanguage(targetLanguage);
            using AudioConfig audioConfig = AudioConfig.FromWavFileInput(audioFile);
            using TranslationRecognizer translator = new TranslationRecognizer(translationConfig, audioConfig);

            //IT DOES NOT WORK
            translator.Synthesizing += (_, e) =>
            {
                var audio = e.Result.GetAudio();
                Console.WriteLine($"Audio synthesized: {audio.Length:#,0} byte(s) {(audio.Length == 0 ? "(Complete)" : "")}");

                if (audio.Length > 0)
                {
                    File.WriteAllBytes("EventBasedSynthesisFromAudioFile.wav", audio);
                }
            };

            var result = await translator.RecognizeOnceAsync();
            if (result.Reason == ResultReason.TranslatedSpeech)
            {
                Console.WriteLine($"Recognized: \"{result.Text}\"");
            }
        }
    }
}
