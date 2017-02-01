//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    //Insertion des librairies
    using System.IO.Ports;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.Windows.Input;
    using System;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Timer variable
        System.Windows.Threading.DispatcherTimer myDispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        //Interval de temps
        int interval = 500;
        
        //Position de la main (ouverte/fermée)
        int positionMain = 1500;

        //Angles à mesurés
        double angleHanche, 
               anglePoignet, 
               angleCoude, 
               angleEpaule;

        //Ports des servomoteur { Main, Poignet, Coude, Epaule, Hanche}
        string[] servo = {"16","23","18","19","20"};

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;


        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            //Si le squelette touche/dépasse le bas de l'image capturé
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            //Si le squelette touche/dépasse le haut de l'image capturé
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            //Si le squelette touche/dépasse le gauche de l'image capturé
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            //Si le squelette touche/dépasse le droite de l'image capturé
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //Create timer
            myDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, interval); // 100 Milliseconds 
            myDispatcherTimer.Tick += new EventHandler(Each_Tick);
            myDispatcherTimer.Stop();

            //Initialisation des différents éléments du xml
            init();

            //Init listBox
            for (int i=0; i<=22; i++)
            {
                lstData.Items.Add("???");   //Ajoute des "?" à chaque ligne de la listbox qui sera utilisée
            }

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Démarre le kinect
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    //Porblème de démarrage/connexion
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                // this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Fermeture de la fenêtre et désactivation du kinect
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }
       
        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)

        {
            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);
            

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            //Joints pour récupérés les coordonnées
            Joint MainD = skeleton.Joints[JointType.HandRight];
            Joint PoignetD = skeleton.Joints[JointType.WristRight];
            Joint CoudeD = skeleton.Joints[JointType.ElbowRight];
            Joint EpauleD = skeleton.Joints[JointType.ShoulderRight];
            Joint Hanche = skeleton.Joints[JointType.Spine];

            //Longueurs avec Y
            double longueurPoignetCoude = Math.Sqrt(Math.Pow(PoignetD.Position.Y - CoudeD.Position.Y, 2) + Math.Pow(PoignetD.Position.X - CoudeD.Position.X, 2));
            double longueurPoignetMain = Math.Sqrt(Math.Pow(PoignetD.Position.Y - MainD.Position.Y, 2) + Math.Pow(PoignetD.Position.X - MainD.Position.X, 2));
            double longueurMainCoude = Math.Sqrt(Math.Pow(MainD.Position.Y - CoudeD.Position.Y, 2) + Math.Pow(MainD.Position.Y - CoudeD.Position.Y, 2));
            double longueurPoignetEpaule = Math.Sqrt(Math.Pow(PoignetD.Position.Y - EpauleD.Position.Y, 2) + Math.Pow(PoignetD.Position.X - EpauleD.Position.X, 2));
            double longueurEpauleCoude = Math.Sqrt(Math.Pow(EpauleD.Position.Y - CoudeD.Position.Y, 2) + Math.Pow(EpauleD.Position.X - CoudeD.Position.X, 2));
            double longueurHancheCoudeY = Math.Sqrt(Math.Pow(Hanche.Position.Y - CoudeD.Position.Y, 2) + Math.Pow(Hanche.Position.X - CoudeD.Position.X, 2));
            double longueurHancheEpaule = Math.Sqrt(Math.Pow(EpauleD.Position.Y - Hanche.Position.Y, 2) + Math.Pow(EpauleD.Position.X - Hanche.Position.X, 2));

            //Longueurs avec Z
            double longueurHancheCoudeZ = Math.Sqrt(Math.Pow(Hanche.Position.Z - CoudeD.Position.Z, 2) + Math.Pow(Hanche.Position.X - CoudeD.Position.X, 2));
            double longueurHancheKinect = Math.Sqrt(Math.Pow(0 - Hanche.Position.Z, 2) + Math.Pow(0 - Hanche.Position.X, 2));
            double longueurCoudeKinect = Math.Sqrt(Math.Pow(0 - CoudeD.Position.Z, 2) + Math.Pow(0 - CoudeD.Position.X, 2));

            //Angles
            anglePoignet = Math.Acos((Math.Pow(longueurPoignetCoude, 2) + Math.Pow(longueurPoignetMain, 2) - Math.Pow(longueurMainCoude, 2)) / (2 * longueurPoignetCoude * longueurPoignetMain)) * 180 / Math.PI;
            angleCoude = Math.Acos((Math.Pow(longueurPoignetCoude, 2) + Math.Pow(longueurEpauleCoude, 2) - Math.Pow(longueurPoignetEpaule, 2)) / (2 * longueurPoignetCoude * longueurEpauleCoude)) * 180 / Math.PI;
            angleEpaule = Math.Acos((Math.Pow(longueurEpauleCoude, 2) + Math.Pow(longueurHancheEpaule, 2) - Math.Pow(longueurHancheCoudeY, 2)) / (2 * longueurEpauleCoude * longueurHancheEpaule)) * 180 / Math.PI;
            angleHanche = Math.Acos((Math.Pow(longueurHancheKinect, 2) - Math.Pow(longueurCoudeKinect, 2) + Math.Pow(longueurHancheCoudeZ, 2)) / (2 * longueurHancheKinect * longueurHancheCoudeZ)) * 180 / Math.PI;

            //Affichage valeur
            lstData.Items[0] = "----- Angle -----";
            lstData.Items[1] = "Poignet : " + anglePoignet.ToString();
            lstData.Items[2] = "Coude : " + angleCoude.ToString();
            lstData.Items[3] = "Epaule : " + angleEpaule.ToString();
            lstData.Items[4] = "Hanche : " + angleHanche.ToString();
            lstData.Items[5] = "-- Coordonnees --";
            lstData.Items[6] = "Main {" + (MainD.Position.X).ToString() + ";" + (MainD.Position.Y).ToString() + ";" + (MainD.Position.Z).ToString() + "}";
            lstData.Items[7] = "Poignet {" + (PoignetD.Position.X).ToString() + ";" + (PoignetD.Position.Y).ToString() + ";" + (PoignetD.Position.Z).ToString() + "}";
            lstData.Items[8] = "Coude {" + (CoudeD.Position.X).ToString() + ";" + (CoudeD.Position.Y).ToString() + ";" + (CoudeD.Position.Z).ToString() + "}";
            lstData.Items[9] = "Epaule {" + (EpauleD.Position.X).ToString() + ";" + (EpauleD.Position.Y).ToString() + ";" + (EpauleD.Position.Z).ToString() + "}";
            lstData.Items[10] = "Hanche {" + (Hanche.Position.X).ToString() + ";" + (Hanche.Position.Y).ToString() + ";" + (Hanche.Position.Z).ToString() + "}";
            lstData.Items[11] = "--- Longueurs ---";
            lstData.Items[12] = "Poignet - Main : " + longueurPoignetMain.ToString();
            lstData.Items[13] = "Poignet - Coude : " + longueurPoignetCoude.ToString();
            lstData.Items[14] = "Main - Coude : " + longueurMainCoude.ToString();
            lstData.Items[15] = "Epaule - Coude : " + longueurEpauleCoude.ToString();
            lstData.Items[16] = "Poignet - Epaule : " + longueurPoignetEpaule.ToString();
            lstData.Items[17] = "Hanche - Coude : " + longueurHancheCoudeY.ToString();
            lstData.Items[18] = "Hanche - Epaule : " + longueurHancheEpaule.ToString();

            //Mapping

            if (chkBlocagePoignet.IsChecked == false)
            {
                anglePoignet = 600 + anglePoignet * 10;
            }
            else
            {
                anglePoignet = 2000;
            }

            angleCoude = 600 + angleCoude * 10;
            angleHanche = 600 + angleHanche * 10;

            if (chkMode180.IsChecked == false)
            {
                if (angleEpaule < 130)
                {
                    angleEpaule = 1000;
                }
                else if (angleEpaule > 180)
                {
                    angleEpaule = 1700;
                }
                else
                {
                    angleEpaule = angleEpaule * 14 - 820;
                }

            }
            else
            {
                angleEpaule = 600 + angleEpaule * 10;
            }

            //// Left Leg
            //this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            //// Right Leg
            //this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            //this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            //this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {

            if (checkBoxSeatedMode.IsChecked == true)
            {
                //active le timer
                myDispatcherTimer.Start();
            }
            else
            {
                //désactive le timer
                myDispatcherTimer.Stop();

                //Valeur du slider
                sldWrist.Value = (int)anglePoignet;
                sldElbow.Value = (int)angleCoude;
                sldShoulder.Value = (int)angleEpaule;
                sldHip.Value = (int)angleHanche;
            }

            if (null != this.sensor)
            {

                //Si la kinect est allumé, le tracking est activé
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;

            }
        }

        //Appuie sur le bouton "get port"
        private void btnGetComPort_Click(object sender, EventArgs e)
        {
            getPort();
        }

        private void getPort()
        {
            string[] ArrayComPortsNames = null; //Tableau contenant les noms des Ports détectés
            int index = 0;                      //index du nombre de ports détectés

            ArrayComPortsNames = SerialPort.GetPortNames(); //Insertion dans le tableau des ports détectés

            clearComboBox(cbPorts); //Vide le comboBox

            //Pour chaque port détecté, l'insérer dans le comboBox
            foreach (string port in ArrayComPortsNames)
            {
                cbPorts.Items.Add(port);
                index++;
            }

            //Si au moins 1 port est détecté
            if (index > 0)
            {
                Array.Sort(ArrayComPortsNames);         //Ranger dans l'ordre croissant les noms des ports
                cbPorts.Text = ArrayComPortsNames[0];   //Afficher au comboox le premier port
            }
            else
            {
                cbPorts.Text = "";
            }
        }

        //Vider les comboBox
        private void clearComboBox(System.Windows.Controls.ComboBox cb)
        {
            cb.Items.Clear();
        }

        private void init()
        {
            //Obtenir les ports visibles
            getPort();

            //Init comboBox contenant les baud rates
            cbBaudRate.Items.Add(300);
            cbBaudRate.Items.Add(600);
            cbBaudRate.Items.Add(1200);
            cbBaudRate.Items.Add(2400);
            cbBaudRate.Items.Add(9600);
            cbBaudRate.Items.Add(14400);
            cbBaudRate.Items.Add(19200);
            cbBaudRate.Items.Add(38400);
            cbBaudRate.Items.Add(57600);
            cbBaudRate.Items.Add(115200);
            cbBaudRate.Items.ToString();

            //Affichage du baud rate à 9600
            cbBaudRate.Text = cbBaudRate.Items[4].ToString();

            //Init comboBox data bits
            cbDataBits.Items.Add(7);
            cbDataBits.Items.Add(8);
            cbDataBits.Items.ToString();
            //Affichae du data bits à 8
            cbDataBits.Text = cbDataBits.Items[1].ToString();

            //Init des comboBox contenant les modes
            cbHandShaking.Items.Add("None");
            cbHandShaking.Items.Add("XOnXOff");
            cbHandShaking.Items.Add("RequestToSend");
            cbHandShaking.Items.Add("RequestToSendXOnXOff");
            //Affichage du modes en "None"
            cbHandShaking.Text = cbHandShaking.Items[0].ToString();

            //Init des comboBox contenant les modes
            cbParity.Items.Add("None");
            cbParity.Items.Add("Mark");
            cbParity.Items.Add("Even");
            cbParity.Items.Add("Odd");
            cbParity.Items.Add("Space");
            //Affichage du modes en "None"
            cbParity.Text = cbParity.Items[0].ToString();

            //Init des comboBox contenant les modes
            cbStopBits.Items.Add("None");
            cbStopBits.Items.Add("One");
            cbStopBits.Items.Add("OnePointFive");
            cbStopBits.Items.Add("Two");
            //Affichage du modes en "None"
            cbStopBits.Text = cbStopBits.Items[1].ToString();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {

            int length = (txtMessage.Text).Length;

            if (txtMessage.Text != "" && cbPorts.Text != "")
            {

                // Création d'un port
                SerialPort _serialPort = new SerialPort();

                //Envoie de la trame
                sendTrame(txtMessage.Text);

                //Supprimer le texte envoyé
                txtMessage.Text = "";
            }
        }

        private void sendTrame(string trame)
        {
            // Création d'une variable port série
            SerialPort _serialPort = new SerialPort();

            //Si aucun port est trouvé ou assigné, arréter
            if (cbPorts.Text == "") return;

            // Allouer les paramètres du port série
            _serialPort.PortName = cbPorts.Text;
            _serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
            _serialPort.DataBits = Convert.ToInt16(cbDataBits.Text);
            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), cbParity.Text);
            if (cbStopBits.Text != "None") _serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cbStopBits.Text);
            _serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), cbHandShaking.Text);

            //Temps de timeout écriture/lecture
            _serialPort.ReadTimeout = 6000;
            _serialPort.WriteTimeout = 6000;

            //Ajout de la fin de trame (Obligatoire pour le SSC-32U)
            trame += "\r";

            //Si un problème se passe, afficher un message d'erreur
            try
            {
                //Ouvre le port
                _serialPort.Open();

                //Envoi du message
                _serialPort.Write(trame);

                //Fermer le port
                _serialPort.Close();
            }
            catch (System.ArgumentException ex)
            {
                //Message d'erreur
                MessageBox.Show(ex.Message);
                if (chkSend.IsChecked == true || checkBoxSeatedMode.IsChecked == true)
                {
                    chkSend.IsChecked = false;
                    checkBoxSeatedMode.IsChecked = false;
                }
            }
            catch (System.UnauthorizedAccessException error)
            {
                MessageBox.Show(error.Message);
                if (chkSend.IsChecked == true || checkBoxSeatedMode.IsChecked == true)
                {
                    chkSend.IsChecked = false;
                    checkBoxSeatedMode.IsChecked = false;
                }
            }

            //Fermer le Port s'il est ouvert
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }


        private string getTrame(int time)
        {
            //Position des différents servomoteurs
            int Hanche = (int)sldHip.Value;         //Port 20
            int Epaule = (int)sldShoulder.Value;    //Port 19
            int Coude = (int)sldElbow.Value;        //Port 18
            int Poignet = (int)sldWrist.Value;      //Port 17
            int positionMain = 0;                   //Port 16

            //Etat de la main en focntion des boutons radios
            if (rbtClosed.IsChecked == true) positionMain = 2500;
            else positionMain = 1500;

            //Trame à envoyer
            string trame = "#" + servo[0] + "P" + positionMain.ToString() + "T500" +
                           "#" + servo[1] + "P" + Poignet.ToString() +
                           "#" + servo[2] + "P" + Coude.ToString() +
                           "#" + servo[3] + "P" + Epaule.ToString() +
                           "#" + servo[4] + "P" + Hanche.ToString() + 
                           "T" + time.ToString();

            //Retourner la trame
            return trame;
        }

        private void chkClockSpeed_CheckedChanged(object sender, EventArgs e)
        {

            if (chkTimerInterval.IsChecked == true)
            {
                //Activation de la clock manuelle
                interval = (int)sldTimerInterval.Value;
            }
            else
            {
                //Désactivation de la clock manuelle
                interval = 100;
            }

            myDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, interval); // 100 Milliseconds 
        }


        //A chaque valeur de l'interval en milliseconde
        public void Each_Tick(object o, EventArgs sender)
        {
            //Envoi de la trame
            if (checkBoxSeatedMode.IsChecked == true)
            {
                //timer
                int timer = 500;

                //Set focus
                checkBoxSeatedMode.Focus();

                //Trame
                string trame = "#" + servo[0] + "P" + (positionMain.ToString()) +
                               "#" + servo[1] + "P" + ((int)anglePoignet).ToString() +
                               "#" + servo[2] + "P" + ((int)angleCoude).ToString() +
                               "#" + servo[3] + "P" + ((int)angleEpaule).ToString() +
                               "#" + servo[4] + "P" + ((int)angleHanche).ToString() + 
                               "T" + timer.ToString();

                //Texte trame
                lstData.Items[19] = "----- trame -----";
                lstData.Items[20] = trame;

                //Etat de la main
                lstData.Items[21] = "-- Statut main --";
                if (positionMain == 1500)
                {
                    lstData.Items[22] = "Main Ouverte";
                    rbtOpened.IsChecked = true;
                    rbtClosed.IsChecked = false;
                }
                else
                {
                    lstData.Items[22] = "Main Fermée";
                    rbtOpened.IsChecked = false;
                    rbtClosed.IsChecked = true;
                }

                //Envoi de la trame
                sendTrame(trame);
            }
            else
            {
                //Envoi de la trame avec la valeur des sliders
                sendTrame(getTrame(500));
            }
            
        }

        private void chkTimerInterval_Checked(object sender, RoutedEventArgs e)
        {

            if (chkTimerInterval.IsChecked == true)
            {
                //Valeur d'interval du timer
                interval = (int)sldTimerInterval.Value;
            }
            else
            {
                //Valeur d'interval par défaut
                interval = 500;
            }

            //Modifier l'interval de temps du timer
            myDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, interval);

        }

        private void Image_KeyDown(object sender, KeyEventArgs e)
        {
            //Etat de la main
            if (e.Key == Key.O)
            {
                //Ouvert
                positionMain = 1500;
            }
            else if (e.Key == Key.C)
            {
                //Fermée
                positionMain = 2500;
            }

            //Changer l'etat de la main
            if (e.Key == Key.N)
            {
                if (positionMain == 2500)
                {
                    //Ouvert
                    positionMain = 1500;
                }
                else
                {
                    positionMain = 2500;
                }
            }

        }

        private void checkBoxSeatedMode_KeyDown(object sender, KeyEventArgs e)
        {
            //Etat de la main
            if (e.Key == Key.O)
            {
                //Ouvrir la main
                positionMain = 1500;
            }
            else if (e.Key == Key.C)
            {
                //Fermer la main
                positionMain = 2500;
            }

            //Changer l'etat de la main
            if (e.Key == Key.N)
            {
                if (positionMain == 2500)
                {
                    //Ouvert
                    positionMain = 1500;
                }
                else
                {
                    //Fermée 
                    positionMain = 2500;
                }
            }

        }

        private void btnMessage_Click(object sender, RoutedEventArgs e)
        {
            //Envoyer le message entré dans le textbox au bras robotique
            sendTrame(txtMessage.Text);
        }

        private void sldTimerInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Afficher la valeur du slider sur le label
            lblIntervalTemps.Content = ((int)sldTimerInterval.Value).ToString();

            //si le checkbox 'd'interval de temps' est actif
            if (chkTimerInterval.IsChecked == true)
            {
                //l'interval prend la valeur du slider
                interval = (int)sldTimerInterval.Value;
            }

        }

        private void btnSend_Click_1(object sender, RoutedEventArgs e)
        {
            //Même code qu'un tick du timer
            Each_Tick(sender, e);
        }

        private void chkSend_Checked(object sender, RoutedEventArgs e)
        {
            //Statut du checkbox
            if (chkSend.IsChecked == true)
            {
                //Activation du timer
                myDispatcherTimer.Start();
            }
            else
            {
                //Désactivation du timer
                myDispatcherTimer.Stop();
            }
        }

        private void btnPorts_Click(object sender, RoutedEventArgs e)
        {
            //Réaliser à nouveau l'initialisation
            init();
        }
    }
}