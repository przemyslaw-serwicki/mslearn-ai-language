using System;
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

                while (true) // Loop indefinitely
                {
                    Console.WriteLine("Enter a number (1-3) to test transcribe: 1-microphone; 2-quick-audio-file; 3-audio-file-with-longer-pauses");
                    Console.WriteLine("Or type 'quit' to exit."); // Add the option to quit

                    string flow = Console.ReadLine();

                    if (flow.Trim().ToLower() == "quit")  // Check for exit condition
                    {
                        break; // Exit the loop
                    }

                    command = flow.Trim() switch
                    {
                        "1" => await TranscribeCommandFromMicrophone(),
                        "2" => await TranscribeCommandFromAudio(),
                        "3" => await TranscribeCommandFromAudioWithChunks(),
                        _ => "This is my default text to test the synthesizer.",
                    };

                    Console.WriteLine("Enter a number (4-6) to synthesize: 4-SSML with faster filter; 5-SSML with cheerful intonation; 6-Comparing with phonema. Otherwise it will be default synthesis containing your earlier command:");
                    flow = Console.ReadLine();

                    switch (flow.Trim())
                    {
                        case "4":
                            await SynthesizeWithSsmlFaster(command);
                            break;
                        case "5":
                            await SynthesizeWithSsmlCheerful(command);
                            break;
                        case "6":
                            await SynthesizeTomatoTextWithCheerfulAndPhoneme();
                            break;
                        default:
                            await SynthesizeCommand(command);
                            break;
                    }
                }

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
            Console.WriteLine("Enter a 't'->(time) or 'd'->(dream) to choose audio file. By default I would use time file");
            string flow = Console.ReadLine();

            string audioFile = flow.Trim() switch
            {
                "t" => "time.wav",
                "d" => "dream.wav",
                _ => "time.wav",
            };
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            wavPlayer.Play();
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
            Console.WriteLine("Enter a 'g'->(gladiator) or 'w'->(wincrowd) to choose audio file. By default I would use gladiator file");
            string flow = Console.ReadLine();

            string audioFile = flow.Trim() switch
            {
                "g" => "gladiator.wav",
                "w" => "wincrowd.wav",
                _ => "gladiator.wav",
            };
            SoundPlayer wavPlayer = new SoundPlayer(audioFile);
            wavPlayer.Play();
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
                    command = e.Result.Text;
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
            // Return the command
            return command;
        }

        static async Task SynthesizeCommand(string commandToSay)
        {
            string command = "You said before: " + commandToSay;
            // Configure speech synthesis
            speechConfig.SpeechSynthesisVoiceName = "en-GB-LibbyNeural";
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);

            //Custom command you said earlier
            SpeechSynthesisResult speak = await speechSynthesizer.SpeakTextAsync(command);
            if (speak.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine(speak.Reason);
            }
            // Print the response
            Console.WriteLine(command);
        }

        static async Task SynthesizeWithSsmlFaster(string commandToSay)
        {
            string command = "You said before: " + commandToSay;

            // Configure speech synthesis
            speechConfig.SpeechSynthesisVoiceName = "en-GB-LibbyNeural";
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);

            // Synthesize spoken output
            string responseSsml = $@"
     <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US' xmlns:mstts=""https://www.w3.org/2001/mstts"">
        <voice name=""en-US-AriaNeural""> 
            <prosody rate=""+35.00%"">
                And now faster by 35 percent!.
                {command}
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

        static async Task SynthesizeWithSsmlCheerful(string commandToSay)
        {
            string command = "You said before: " + commandToSay;
            // Configure speech synthesis
            speechConfig.SpeechSynthesisVoiceName = "en-GB-LibbyNeural";
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);

            // Synthesize spoken output
            string responseSsml = $@"
<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" 
                     xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US""> 
    <voice name=""en-US-AriaNeural""> 
        <mstts:express-as style=""cheerful""> 
          {command}
        </mstts:express-as> 
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

        static async Task SynthesizeTomatoTextWithCheerfulAndPhoneme()
        {
            // Configure speech synthesis
            speechConfig.SpeechSynthesisVoiceName = "en-GB-LibbyNeural";
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);

            // Synthesize spoken output
            string responseSsml = $@"
<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" 
                     xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US""> 
    <voice name=""en-US-AriaNeural""> 
        <mstts:express-as style=""cheerful""> 
          Cheerful intonation - I say tomato
        </mstts:express-as> 
    </voice> 
    <voice name=""en-US-GuyNeural""> 
        I say <phoneme alphabet=""sapi"" ph=""t ao m ae t ow""> tomato </phoneme>. 
        <break strength=""weak""/>Lets call the whole thing off! 
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
