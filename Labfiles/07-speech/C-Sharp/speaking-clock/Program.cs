﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

// Import namespaces
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Media;


namespace speaking_clock
{
    class Program
    {
        private static SpeechConfig speechConfig;
        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string aiSvcKey = configuration["SpeechKey"];
                string aiSvcRegion = configuration["SpeechRegion"];

                // Configure speech service
                speechConfig = SpeechConfig.FromSubscription(aiSvcKey, aiSvcRegion);
                Console.WriteLine("Ready to use speech service in " + speechConfig.Region);

                // Configure voice
                speechConfig.SpeechSynthesisVoiceName = "en-US-AriaNeural";


                // Get spoken input
                string command = "";
                //command = await TranscribeCommandFromMicrophone();
                command = await TranscribeCommandFromAudio();
                //command = await TranscribeCommandFromAudioWithChunks();
                //await TellCommand(command);
                await TellWithSsml();
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task<string> TranscribeCommandFromMicrophone()
        {
            string command = "";

            // Configure speech recognition microphone
            using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using SpeechRecognizer speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
            Console.WriteLine("Speak now...");

            // Process speech input
            SpeechRecognitionResult speech = await speechRecognizer.RecognizeOnceAsync();
            if (speech.Reason == ResultReason.RecognizedSpeech)
            {
                command = speech.Text;
                Console.WriteLine(command);
            }
            else
            {
                Console.WriteLine(speech.Reason);
                if (speech.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(speech);
                    Console.WriteLine(cancellation.Reason);
                    Console.WriteLine(cancellation.ErrorDetails);
                }
            }


            // Return the command
            return command;
        }

        static async Task<string> TranscribeCommandFromAudio()
        {
            string command = "";

            // Configure speech recognition from an audio file
            string audioFile = "time.wav";
            audioFile = "dream.wav";
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            //wavPlayer.Play();
            using AudioConfig audioConfig = AudioConfig.FromWavFileInput(audioFile);
            using SpeechRecognizer speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // Process speech input
            SpeechRecognitionResult speech = await speechRecognizer.RecognizeOnceAsync();
            if (speech.Reason == ResultReason.RecognizedSpeech)
            {
                command = speech.Text;
                Console.WriteLine(command);
            }
            else
            {
                Console.WriteLine(speech.Reason);
                if (speech.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(speech);
                    Console.WriteLine(cancellation.Reason);
                    Console.WriteLine(cancellation.ErrorDetails);
                }
            }


            // Return the command
            return command;
        }

        static async Task<string> TranscribeCommandFromAudioWithChunks()
        {
            string command = "";

            // Configure speech recognition from an audio file
            string audioFile = "gladiator.wav";
            //audioFile = "wincrowd.wav";
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            //wavPlayer.Play();
            using AudioConfig audioConfig = AudioConfig.FromWavFileInput(audioFile);
            using SpeechRecognizer speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            //Managing longer audio with pauses
            var stopRecognition = new TaskCompletionSource<int>();
            speechRecognizer.Recognizing += (s, e) =>
            {
                Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            };

            speechRecognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
            };

            speechRecognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }

                stopRecognition.TrySetResult(0);
            };

            speechRecognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\n    Session stopped event.");
                stopRecognition.TrySetResult(0);
            };

            await speechRecognizer.StartContinuousRecognitionAsync();

            // Waits for completion. Use Task.WaitAny to keep the task rooted.
            Task.WaitAny(new[] { stopRecognition.Task });

            // Make the following call at some point to stop recognition:
            await speechRecognizer.StopContinuousRecognitionAsync();
            //

            // Process speech input
            //SpeechRecognitionResult speech = await speechRecognizer.RecognizeOnceAsync();
            //if (speech.Reason == ResultReason.RecognizedSpeech)
            //{
            //    command = speech.Text;
            //    Console.WriteLine(command);
            //}
            //else
            //{
            //    Console.WriteLine(speech.Reason);
            //    if (speech.Reason == ResultReason.Canceled)
            //    {
            //        var cancellation = CancellationDetails.FromResult(speech);
            //        Console.WriteLine(cancellation.Reason);
            //        Console.WriteLine(cancellation.ErrorDetails);
            //    }
            //}


            // Return the command
            return command;
        }

        static async Task TellCommand(string commandToSay)
        {
            var now = DateTime.Now;
            string responseText = "The time is " + now.Hour.ToString() + ":" + now.Minute.ToString("D2");

            // Configure speech synthesis
            speechConfig.SpeechSynthesisVoiceName = "en-GB-LibbyNeural";
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);


            // Synthesize spoken output
            SpeechSynthesisResult speak = await speechSynthesizer.SpeakTextAsync(responseText);
            if (speak.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine(speak.Reason);
            }

            //Custom command you said earlier
            speak = await speechSynthesizer.SpeakTextAsync("You said before" + commandToSay);
            if (speak.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine(speak.Reason);
            }


            // Print the response
            Console.WriteLine(responseText);
            Console.WriteLine("You said before" + commandToSay);
        }

        static async Task TellWithSsml()
        {
            var now = DateTime.Now;
            string responseText = "The time is " + now.Hour.ToString() + ":" + now.Minute.ToString("D2");

            // Configure speech synthesis
            speechConfig.SpeechSynthesisVoiceName = "en-GB-LibbyNeural";
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);


            // Synthesize spoken output
            string responseSsml = $@"
     <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US' xmlns:mstts=""https://www.w3.org/2001/mstts"">
        <voice name=""en-US-AriaNeural""> 
            <mstts:express-as style=""cheerful"" styledegree=""2""> 
              I am talking to you right now in cheerful intonation
            </mstts:express-as> 
            <prosody rate=""+35.00%"">
                And now faster by 35 percent - this is fast!.
                {responseText}
                 <break strength='strong'/>
                 Time to end this lab!
            </prosody>
        </voice> 
     </speak>";
            SpeechSynthesisResult speak = await speechSynthesizer.SpeakSsmlAsync(responseSsml);
            if (speak.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine(speak.Reason);
            }


            // Print the response
            Console.WriteLine(responseSsml);
        }

    }
}
