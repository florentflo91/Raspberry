using System;
using System.Diagnostics;
using System.IO;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;
using System.Text;
using Windows.Globalization;
using System.Net.Http;
using System.Xml;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Windows.Media.Playback;
using Windows.Media.Core;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FloAssistant
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public  partial class MainPage : Page
    {
        internal static readonly string LogFile = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "FloAssistant.log");
        private SpeechSynthesizer synthesizer;
        private SpeechRecognizer speechRecognizer;
        private StringBuilder dictatedTextBuilder = new StringBuilder();
        private static uint HResultPrivacyStatementDeclined = 0x80045509;
        private DB db;
        private LiveboxServer livevoxServer;
        //Initialise la base de données
        private string dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "db.sqlite");
        
        //Livebox Server
        private DispatcherTimer timer_; //Timer qui check si il y a un nouveau device de connecté

        public MainPage()
        {             
            this.InitializeComponent();
            
            InitDatabase();
            //InitLiveboxServer(); //Initialise la connection à la Livebox Server
            InitSynthetizer(); //Initialise la voix de Cortana
            InitRecognizer(); //Initialise la reconnaissance vocale
            
            Speak("Flo assistant, bonjour !");
            //GetNews();
            //Radio("a");
            ContinuousRecognize_Click(null,null);
        }

        private void InitDatabase()
        {
            DB db = new DB(dbPath);
            this.db = db;
            Log("dbPath : " + dbPath);
        }

        public void InitLiveboxServer()
        {
            Log("Init Livebox Server");
            timer_ = new DispatcherTimer();
            timer_.Interval = TimeSpan.FromSeconds(5);
            timer_.Tick += GetDevices;
            timer_.Start();
        }

        private void InitSynthetizer()
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

        public void IA(string texte)
        {
            string texteLower = texte.ToLower();
            if (texteLower == "bonjour")
            {
                Speak("Bonjour");
            }
            else if (texteLower.Contains("météo") || texteLower.Contains("température") || texteLower.Contains("temps"))
            {
                Meteo(texte);
            }
            else if (texteLower.Contains("radio"))
            {
                Radio(texte);
            }
            else if (texteLower.Contains("mila") || texteLower.Contains("mets-la") || texteLower.Contains("mets la") || texteLower.Contains("mais là") || texteLower.Contains("tv") || texteLower.Contains("télé"))
            {
                LiveboxTV(texte);
            }
            else if (texteLower.Contains("qu'en penses-tu"))
            {
                Speak("C'est trés bien");
            }
            else if (texteLower.Contains("t'appelles-tu"))
            {
                Speak("Je m'appelle XA.");
            }
            else if (texteLower.Contains("heure"))
            {
                string heure = DateTime.Now.ToString("HH") + " heure" + DateTime.Now.ToString("mm") + ".";
                Speak("Il est : " + heure);
            }
            else if (texteLower.Contains("news"))
            {
                GetNews();
            }
        }

        private async void GetDevices(object sender, object args)
        {
            Log("Start :  GetDevices");
            LiveboxServer livevoxServer = new LiveboxServer(db);

            var resultMobileStateList = await livevoxServer.DetectConnectedDevices();

            foreach(var mobileState in resultMobileStateList)
            {
                if (mobileState.StateChanged == true && mobileState.State == "Online")
                {
                    Speak("Bonjour " + mobileState.Owner + " !");
                }
                else if (mobileState.StateChanged == true && mobileState.State == "Offline")
                {
                    Speak("Au revoir " + mobileState.Owner + " !");
                }
            }
            Log("End :  GetDevices");
        }

        private async void GetNews()
        {
            Log("Start :  GetNews");
            News news = new News();
            List<News.Article> listNews = await news.GetNews();
           
            string articleToSpeak = null;

            foreach (News.Article article in listNews)
            {                          

                articleToSpeak = articleToSpeak + ". " + article.title;
               // 
               //Speak(article.title);
            }
            Log(articleToSpeak);
            if (media.CurrentState == MediaElementState.Playing)
            {
                Log(media.CurrentState.ToString());
                await Task.Delay(TimeSpan.FromSeconds(1));
                Log(media.CurrentState.ToString());
            }
            Speak(articleToSpeak);
            Log("End :  GetNews");
        }

        public void Radio(string texte) {

            MediaPlayer radioMediaElement = new MediaPlayer();
            radioMediaElement.Source = MediaSource.CreateFromUri(new Uri("http://alouette.ice.infomaniak.ch/alouette-high.mp3"));
            radioMediaElement.Play();
        }
        /// <summary>
        /// Begin recognition, or finish the recognition session. 
        /// </summary>
        /// <param name="sender">The button that generated this event</param>
        /// <param name="e">Unused event details</param>
        public async void ContinuousRecognize_Click(object sender, RoutedEventArgs e)
        {
            if (speechRecognizer.State == SpeechRecognizerState.Idle)
            {
                try
                {
                    await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                }
                catch (Exception ex)
                {
                    Log("ERROR : " + ex.ToString());
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
                        //SetTextToTextBox("dictationTextBox", dictatedTextBuilder.ToString());
                        dictationTextBox.Text = dictatedTextBuilder.ToString();
                    }
                    catch (Exception exception)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
                else{

                    Log("Idle");
                }
            }
            //btnContinuousRecognize.IsEnabled = true;
        }

        public async void Speak(string texte)
        {
            if (media.CurrentState == MediaElementState.Playing)
            {
                media.Stop();
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
                        media.AutoPlay = true;
                        media.SetSource(synthesisStream, synthesisStream.ContentType);
                        media.Play();
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
                        media.AutoPlay = false;
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Unable to synthesize text");
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            Log("ContinuousRecognitionSession_Completed");
            //MainPage mainpage = new MainPage();
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                // If TimeoutExceeded occurs, the user has been silent for too long. We can use this to 
                // cancel recognition if the user in dictation mode and walks away from their device, etc.
                // In a global-command type scenario, this timeout won't apply automatically.
                // With dictation (no grammar in place) modes, the default timeout is 20 seconds.
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {
                    Log("TimeoutExceeded");
                    ContinuousRecognize_Click(null, null);
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                     {
                    //Log("Automatic Time Out of Dictation");
                    
                        dictationTextBox1.Text = dictatedTextBuilder.ToString();
                    
                    //SetTextToTextBox("dictationTextBox1", dictatedTextBuilder.ToString());
                   
                    // isListening = false;
                    });
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
            Log("SpeechRecognizer_HypothesisGenerated");
            //MainPage mainpage = new MainPage();
            string hypothesis = args.Hypothesis.Text;
            Log("HypothesisGenerated : " + args.Hypothesis.Text);
            // Update the textbox with the currently confirmed text, and the hypothesis combined.
            string textboxContent = dictatedTextBuilder.ToString() + " " + hypothesis + " ...";
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                dictationTextBox2.Text = textboxContent;
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
            try
            {
                Log("ContinuousRecognitionSession_ResultGenerated");
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
                       dictationTextBox1.Text = dictatedTextBuilder.ToString();
                     //SetTextToTextBox("dictationTextBox1", args.Result.Text);
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
                    dictationTextBox1.Text = dictatedTextBuilder.ToString();
                    //SetTextToTextBox("dictationTextBox1", dictatedTextBuilder.ToString());

                    string discardedText = args.Result.Text;
                    if (!string.IsNullOrEmpty(discardedText))
                    {
                        discardedText = discardedText.Length <= 25 ? discardedText : (discardedText.Substring(0, 25) + "...");
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            discardedTextBlock.Text = "Discarded due to low/rejected Confidence: " + discardedText;
                        //SetTextToTextBox("discardedTextBlock", "Discarded due to low/rejected Confidence: " + discardedText);
                        // SetVisibilityToTextBox("discardedTextBlock", Visibility.Visible);
                        discardedTextBlock.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        });
                    }
                    // });
                }
            }
            catch(Exception ex)
            {
                Log(ex.ToString());

            }
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

       

        public string SupprimeDeterminant(string texte){

            texte = texte.Replace(" le ", "").Replace("Le ", "");
            texte = texte.Replace(" la ", "").Replace("La ", "");
            //texte = texte.Replace("les", "");
            texte = texte.Replace("un", "");
            texte = texte.Replace("une", "");
            texte = texte.Replace("des", "");
            return texte;

        }

        public string SupprimeConjoctionCoordination(string texte)
        {

            texte = texte.Replace("mais", "");
            texte = texte.Replace("où", "");
            texte = texte.Replace("et", "");
            texte = texte.Replace("donc", "");
            texte = texte.Replace("or", "");
            texte = texte.Replace("ni", "");
            texte = texte.Replace("car", "");
            return texte;

        }

        public string SupprimeDetrminantsPronomInterrogatif(string texte)
        {

            texte = texte.Replace("qui", "");
            
            texte = texte.Replace("quoi", "");
            texte = texte.Replace("lequel", "");
            texte = texte.Replace(" quelle ", "").Replace("Quelle ", "");
            texte = texte.Replace(" quel ", "").Replace("Quel ", "");
            texte = texte.Replace("que", "");
            return texte;

        }

        public string SupprimeVerbeEtreAvoir(string texte)
        {

            texte = texte.Replace("est", "");
            texte = texte.Replace("sont", "");
            //texte = texte.Replace("a", "");
            texte = texte.Replace("ont", "");
            return texte;

        }

        public string SupprimeVerbeFaire(string texte)
        {

            texte = texte.Replace("fait-il", "");
            texte = texte.Replace("fera-t-il", "");
            return texte;

        }

        public string SupprimePreposition(string texte)
        {

            texte = texte.Replace("à", "");
            texte = texte.Replace("de", "");
            texte = texte.Replace(" par ", "");
            texte = texte.Replace("vers", "");
            return texte;

        }

        public string AdjectifDemonstratif(string texte)
        {

            texte = texte.Replace(" cette ", "");
            texte = texte.Replace(" cet", "");
            texte = texte.Replace(" ce ", "");
            return texte;

        }

        public string SupprimeAverbeTemps(string texte)
        {

            texte = texte.Replace("aujourd'hui", "");
            texte = texte.Replace("demain", "");
            texte = texte.Replace("hier", "");
            texte = texte.Replace("soir", "");
            texte = texte.Replace("matin", "");
            texte = texte.Replace("après-midi", "");
            texte = texte.Replace("midi", "");
            return texte;

        }

        private async void Meteo(string texte)
        {
            Log("Ordre : Météo, " + texte);
            string ville = SupprimeConjoctionCoordination(AdjectifDemonstratif(SupprimeVerbeFaire(SupprimePreposition(SupprimeAverbeTemps(SupprimeVerbeEtreAvoir(SupprimeDetrminantsPronomInterrogatif(SupprimeDeterminant(texte))))))));
            ville = ville.Replace("Météo.", "").Replace("température", "").Replace("météo", "").Replace("temps", "").Replace("Météo", "").Trim();
            string villeSansAccent = ville.Replace("é", "e").Replace("è", "e");
            if (ville == "") { ville = "igny"; villeSansAccent = "igny"; }
            Log("Ville : " + ville);
            Log("Ville sans accents : " + villeSansAccent);
            string url = "http://api.openweathermap.org/data/2.5/forecast?q=" + villeSansAccent + "&mode=xml&units=metric&APPID=2660adb50b8dece7892bfaffd75a069a&lang=fr";
            Log("Url : " + url);
            Uri requestUri = new Uri(url);
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            string temperature = "";
            string clouds = "";
            string debutTexte = "";

            try
            {
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Log(httpResponseBody);
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(httpResponseBody);

                if (texte.Contains("demain"))
                {
                    if (texte.Contains("matin"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.AddDays(1).ToString("yyyy-MM-ddT06:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Demain matin, ";
                            }
                        }
                    }
                    else if (texte.Contains("après-midi"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.AddDays(1).ToString("yyyy-MM-ddT12:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Demain aprés-midi, ";
                            }
                        }
                    }
                    else if (texte.Contains("soir"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.AddDays(1).ToString("yyyy-MM-ddT18:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Demain soir, ";
                            }
                        }
                    }
                    else if (texte.Contains("midi"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.AddDays(1).ToString("yyyy-MM-ddT09:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Demain midi, ";
                            }
                        }
                    }
                    else
                    {

                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.AddDays(1).ToString("yyyy-MM-ddT09:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Demain, ";
                            }
                        }
                    }
                }
                else
                {
                    if (texte.Contains("matin"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.ToString("yyyy-MM-ddT06:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Ce matin, ";
                            }
                        }
                    }
                    else if (texte.Contains("après-midi"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.ToString("yyyy-MM-ddT12:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Cet aprés-midi, ";
                            }
                        }
                    }
                    else if (texte.Contains("midi"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.ToString("yyyy-MM-ddT09:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Ce midi, ";
                            }
                        }
                    }
                    else if (texte.Contains("soir"))
                    {
                        foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        {
                            if (chldNode.Attributes["from"].Value == DateTime.Now.ToString("yyyy-MM-ddT18:00:00"))
                            {
                                temperature = chldNode["temperature"].Attributes["value"].Value;
                                clouds = chldNode["clouds"].Attributes["value"].Value;
                                debutTexte = "Ce soir, ";
                            }
                        }
                    }
                    else
                    {

                        //foreach (XmlNode chldNode in doc.DocumentElement.GetElementsByTagName("time"))
                        //{
                        XmlNode chldNode = doc.DocumentElement.GetElementsByTagName("time")[0]; //== DateTime.Now.ToString("yyyy-MM-ddT21:00:00"))
                                                                                                //{
                        temperature = chldNode["temperature"].Attributes["value"].Value;
                        clouds = chldNode["clouds"].Attributes["value"].Value;
                        debutTexte = "";
                        //}
                        //}
                    }
                }
            }
            catch
            {
                Speak("Impossible de comprendre ce que tu as dis, désolé !");

            }
            Log("temperature : " + temperature);
            if (temperature == "")
            {
                Speak("Impossible de déterminer la température, désolé !");

            }
            else
            {
                double temperatureInt = Math.Round(double.Parse(temperature.Replace(".", ",")), 1);
                string temperatureString = temperatureInt.ToString().Replace(",", " virgule ");
                Speak(debutTexte + " la température est de " + temperatureString + " degré à " + ville + ". Le temps sera " + clouds + ".");
            }
            
                     
        }

        private async void LiveboxTV(string texte)
        {
            Log("Ordre : LiveboxTV, " + texte);

            string[] commands = null;

            if (texte.ToLower().Contains("mila") || texte.ToLower().Contains("mets-la") || texte.ToLower().Contains("mets la") || texte.ToLower().Contains("mais là"))
            {
                

                string chaine = null; // texte.ToLower().Replace("mila", "").Replace("mets la", "").Replace("mets-la", "").Replace("mais là", "").Trim();
                chaine = chaine.Replace("une", "1").Replace("un", "1");

                Regex regex = new Regex(@"\d+");
                Match chaineRegex = regex.Match(texte);
                if (chaineRegex.Success)
                {
                    Log("Chaine Regex : " + chaineRegex.Value);
                    chaine = chaineRegex.Value;
                }

                Log("Chaine : " + chaine);
                var isNumeric = int.TryParse(chaine, out int n);
                Log(isNumeric.ToString());
                Log(n.ToString());

                if (isNumeric) {
                    int length = n.ToString().Length;
                   commands = new string[length];
                    Log("length : " + length.ToString());
                    for (int i = length-1; i >= 0; i--)
                    {
                        Log(i.ToString());
                        Log(((n % 10) + 512).ToString());
                        commands[i] = ((n % 10) + 512).ToString();
                        n /= 10;
                    }
                }
            }
            else if(texte.Contains("tv") || texte.Contains("télé"))
            {
                if (texte.Contains("programme"))
                {
                    if (texte.Contains("soir"))
                    {
                        commands = new string[] { "139", "106", "352", "352" };
                    }
                    else
                    {
                        commands = new string[] { "139", "106", "106", "352", "352" };
                    }
                }
                else if (texte.Contains("allume"))
                {
                    commands = new string[] { "116", "352" };
                }
                else if (texte.Contains("éteins"))
                {
                    commands = new string[] { "116" };
                }
            }
           



            if (commands != null)
            {
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage httpResponse = new HttpResponseMessage();
                foreach (string command in commands)
                {
                    string url = "http://192.168.1.10:8080/remoteControl/cmd?operation=01&key=" + command + "&mode=0";//touche 1";
                    Log("Url : " + url);
                    Uri requestUri = new Uri(url);
                    string httpResponseBody = "";

                    try
                    {
                        httpResponse = await httpClient.GetAsync(requestUri);
                        httpResponse.EnsureSuccessStatusCode();
                        httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                        Log(httpResponseBody);
                    }
                    catch (Exception ex)
                    {
                        httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                    }

                }
            }
        }

        public  void Log(string logMessage, FileStream logFile1 = null)
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