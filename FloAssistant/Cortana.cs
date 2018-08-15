using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;


namespace FloAssistant
{
    public class Cortana  : MainPage
    {
        private SpeechSynthesizer synthesizer;
        private SpeechRecognizer speechRecognizer;
        //internal static readonly string LogFile = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "Cortana.log");
        public StringBuilder dictatedTextBuilder;
        private static uint HResultPrivacyStatementDeclined = 0x80045509;

        public Cortana()
        {
           // InitSynthetizer();
            //InitRecognizer();
            //dictatedTextBuilder = new StringBuilder();
        }
        public void InitSynthetizer()
        {
            synthesizer = new SpeechSynthesizer();
            foreach (VoiceInformation voice in SpeechSynthesizer.AllVoices)
            {
                Log("Liste voix : " + voice.DisplayName);
                if (voice.DisplayName == "Microsoft Julie")
                {
                    synthesizer.Voice = voice;
                    Log("Voix sélectionnée : " + voice.DisplayName);
                }
            }
        }
        private async void InitRecognizer()
        {
            Language recognizerLanguage = new Windows.Globalization.Language("fr-FR");
            if (speechRecognizer != null)
            {
                // cleanup prior to re-initializing this scenario.
                speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
                speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
                speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }

            this.speechRecognizer = new SpeechRecognizer(recognizerLanguage);

            // Provide feedback to the user about the state of the recognizer. This can be used to provide visual feedback in the form
            // of an audio indicator to help the user understand whether they're being heard.
            speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;

            // Apply the dictation topic constraint to optimize for dictated freeform speech.
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                //rootPage.NotifyUser("Grammar Compilation Failed: " + result.Status.ToString(), NotifyType.ErrorMessage);
                //btnContinuousRecognize.IsEnabled = false;
            }

            // Handle continuous recognition events. Completed fires when various error states occur. ResultGenerated fires when
            // some recognized phrases occur, or the garbage rule is hit. HypothesisGenerated fires during recognition, and
            // allows us to provide incremental feedback based on what the user's currently saying.
            speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;
        }

        /// <summary>
        /// Provide feedback to the user based on whether the recognizer is receiving their voice input.
        /// </summary>
        /// <param name="sender">The recognizer that is currently running.</param>
        /// <param name="args">The current state of the recognizer.</param>
        private async void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            //MainPage mainpage = new MainPage();
            //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
            Log("State : " + args.State.ToString());
            //});
        }

        private void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            //MainPage mainpage = new MainPage();
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                // If TimeoutExceeded occurs, the user has been silent for too long. We can use this to 
                // cancel recognition if the user in dictation mode and walks away from their device, etc.
                // In a global-command type scenario, this timeout won't apply automatically.
                // With dictation (no grammar in place) modes, the default timeout is 20 seconds.
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {

                    //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    // {
                    //Log("Automatic Time Out of Dictation");
                    
                    //dictationTextBox1.Text = dictatedTextBuilder.ToString();

                    SetTextToTextBox("dictationTextBox1", dictatedTextBuilder.ToString());

                    // isListening = false;
                    //});
                }
                else
                {
                    //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    //{
                    Log("Continuous Recognition Completed: " + args.Status.ToString());
                    //DictationButtonText.Text = " Dictate";
                    //SetTextToTextBox("dictationTextBox1", dictatedTextBuilder.ToString());

                    //dictationTextBox1.Text = dictatedTextBuilder.ToString();

                    //cbLanguageSelection.IsEnabled = true;
                    //isListening = false;
                    // });
                }
            }
            else
            {
                Log("toto");
                ContinuousRecognize_Click(null, null);
            }
        }

        /// <summary>
        /// While the user is speaking, update the textbox with the partial sentence of what's being said for user feedback.
        /// </summary>
        /// <param name="sender">The recognizer that has generated the hypothesis</param>
        /// <param name="args">The hypothesis formed</param>
        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            //MainPage mainpage = new MainPage();
            string hypothesis = args.Hypothesis.Text;
            Log("HypothesisGenerated : " + args.Hypothesis.Text);
            // Update the textbox with the currently confirmed text, and the hypothesis combined.
            string textboxContent = dictatedTextBuilder.ToString() + " " + hypothesis + " ...";
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //dictationTextBox2.Text = textboxContent;
                 SetTextToTextBox("dictationTextBox2", "yo");
                //btnClearText.IsEnabled = true;
            });
        }

        /// <summary>
        /// Handle events fired when a result is generated. Check for high to medium confidence, and then append the
        /// string to the end of the stringbuffer, and replace the content of the textbox with the string buffer, to
        /// remove any hypothesis text that may be present.
        /// </summary>
        /// <param name="sender">The Recognition session that generated this result</param>
        /// <param name="args">Details about the recognized speech</param>
        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // MainPage mainpage = new MainPage();
            // We may choose to discard content that has low confidence, as that could indicate that we're picking up
            // noise via the microphone, or someone could be talking out of earshot.
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High)
            {
                dictatedTextBuilder.Append(args.Result.Text + " ");

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    IA(args.Result.Text);
                    //discardedTextBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    Log(args.Result.Text);
                    //dictationTextBox1.Text = dictatedTextBuilder.ToString();
                    SetTextToTextBox("dictationTextBox1", args.Result.Text);
                    //btnClearText.IsEnabled = true;
                });
            }
            else
            {
                // In some scenarios, a developer may choose to ignore giving the user feedback in this case, if speech
                // is not the primary input mechanism for the application.
                // Here, just remove any hypothesis text by resetting it to the last known good.
                //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //{
                //dictationTextBox1.Text = dictatedTextBuilder.ToString();
                SetTextToTextBox("dictationTextBox1", dictatedTextBuilder.ToString());

                string discardedText = args.Result.Text;
                if (!string.IsNullOrEmpty(discardedText))
                {
                    discardedText = discardedText.Length <= 25 ? discardedText : (discardedText.Substring(0, 25) + "...");
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        //discardedTextBlock.Text = "Discarded due to low/rejected Confidence: " + discardedText;
                        SetTextToTextBox("discardedTextBlock", "Discarded due to low/rejected Confidence: " + discardedText);
                         SetVisibilityToTextBox("discardedTextBlock", Visibility.Visible);
//discardedTextBlock.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    });
                }
                // });
            }
        }

        /// <summary>
        /// Begin recognition, or finish the recognition session. 
        /// </summary>
        /// <param name="sender">The button that generated this event</param>
        /// <param name="e">Unused event details</param>
        public async void ContinuousRecognize()
        {
            //MainPage mainpage = new MainPage();
            if (speechRecognizer.State == SpeechRecognizerState.Idle)
            {
                try
                {
                    await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                }
                catch (Exception ex)
                {
                    if ((uint)ex.HResult == HResultPrivacyStatementDeclined)
                    {
                        // Show a UI link to the privacy settings.
                        //hlOpenPrivacySettings.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
            }
            else
            {
                if (speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    try
                    {
                        await speechRecognizer.ContinuousRecognitionSession.StopAsync();

                        // Ensure we don't leave any hypothesis text behind
                        MainPage.gui.SetTextToTextBox("discardedTextBlock", dictatedTextBuilder.ToString());
                        //dictationTextBox.Text = dictatedTextBuilder.ToString();
                    }
                    catch (Exception exception)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
            }
            //btnContinuousRecognize.IsEnabled = true;
        }

        public async void Speak(string texte)
        {
            


            if (GetMediaCurrentState() == MediaElementState.Playing)
            {
                //MainPage.Stop
                   //MainPage  mainpage = new MainPage();
                StopMedia();
                //MainPage.UI.window
                //UI.window.
            }
            else
            {
                if (!String.IsNullOrEmpty(texte))
                {
                    try
                    {
                        Log("Speak : " + texte);
                        // Create a stream from the text. This will be played using a media element.
                        SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeTextToStreamAsync(texte);

                        // Set the source and start playing the synthesized audio stream.
                        SetMediaAutoplay(true);
                        SetMediaSource(synthesisStream);
                        PlayMedia();
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        // If media player components are unavailable, (eg, using a N SKU of windows), we won't
                        // be able to start media playback. Handle this gracefully                      
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Media player components unavailable");
                        await messageDialog.ShowAsync();
                    }
                    catch (Exception)
                    {
                        // If the text is unable to be synthesized, throw an error message to the user.
                        SetMediaAutoplay(false);
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Unable to synthesize text");
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }

        public void Log(string logMessage, FileStream logFile1 = null)
        {
            // Append text to an existing file named "WriteLines.txt".
            //FileStream logFile1 = System.IO.File.AppendText(LogFile);
            Debug.WriteLine(DateTime.Now.ToString() + " " + logMessage);
            using (StreamWriter outputFile = File.AppendText(LogFile))
            {

                outputFile.WriteLine(DateTime.Now.ToString() + " " + logMessage);
                outputFile.Dispose();
            }
        }
    }
}
