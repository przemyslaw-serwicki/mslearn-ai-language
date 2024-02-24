using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

// Import namespaces
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;


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
                command = await TranscribeCommand();
                await TellTime(command);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task<string> TranscribeCommand()
        {
            string command = "";

            // Configure speech recognition
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

        static async Task TellTime(string commandToSay)
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

            string responseSsml = $@"
     <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US' xmlns:mstts=""https://www.w3.org/2001/mstts"">
    <voice name=""en-US-AriaNeural""> 
        <mstts:express-as style=""cheerful"" styledegree=""2""> 
          I will say what time is it
        </mstts:express-as> 
    </voice> 
         <voice name='en-GB-LibbyNeural'>
             {responseText}
             <break strength='strong'/>
             Time to end this lab!
         </voice>
     </speak>";
            speak = await speechSynthesizer.SpeakSsmlAsync(responseSsml);
            if (speak.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine(speak.Reason);
            }


            // Print the response
            Console.WriteLine(responseText);
            Console.WriteLine("You said before" + commandToSay);
            Console.WriteLine(responseSsml);
        }

    }
}
