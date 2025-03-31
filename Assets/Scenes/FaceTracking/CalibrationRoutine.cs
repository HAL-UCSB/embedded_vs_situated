using System.Collections;
using TMPro;
using UnityEngine.Assertions;

namespace UnityEngine.XR.ARFoundation.Samples
{

    public enum CalibrationPhase
    {
        Baseline = 0,
        Smile,
        EyebrowRaise,

        ReverseFrown,
        End
    }
    public class CalibrationRoutine : MonoBehaviour
    {
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI timerText;

        CalibrationPhase calibrationPhase;
        CalibrationPhase nextCalibrationPhase;
        bool timerRunning = false;

        bool calibrationStarted = false;
        LandmarkMovingAverageFilter landmarkMovingAverage = new LandmarkMovingAverageFilter(10);

        AudioSource audioSource;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            calibrationPhase = CalibrationPhase.Baseline;
            nextCalibrationPhase = CalibrationPhase.Baseline;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            audioSource = GetComponent<AudioSource>();
            Assert.IsNotNull(audioSource);
            Assert.IsNotNull(timerText);
        }

        public void StartCalibration()
        {
            calibrationStarted = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!calibrationStarted)
            {
                return;
            }
            if (calibrationPhase == CalibrationPhase.End)
            {
                SceneManagement.SceneManager.LoadScene(1);
            }
            if (!timerRunning)
            {
                switch (nextCalibrationPhase)
                {
                    case CalibrationPhase.Baseline:
                        // instructionText.text = "Please keep a neutral expression";
                        timerText.text = "Please keep a neutral expression for ";
                        StartCoroutine(CountdownTimer(10));
                        break;
                    case CalibrationPhase.Smile:
                        if (nextCalibrationPhase != calibrationPhase)
                        {
                            // instructionText.text = "Get ready to smile";
                            timerText.text = "Get ready to smile in ";
                            // calibrationPhase = CalibrationPhase.Smile;
                            StartCoroutine(CountdownTimer(5));
                        }
                        else
                        {
                            landmarkMovingAverage.Reset();
                            // instructionText.text = "Please smile";
                            timerText.text = "Please smile for ";
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;
                    case CalibrationPhase.EyebrowRaise:
                        if (nextCalibrationPhase != calibrationPhase)
                        {
                            // instructionText.text = "Get ready to raise brows";
                            timerText.text = "Get ready to raise brows in ";
                            // calibrationPhase = CalibrationPhase.EyebrowRaise;
                            StartCoroutine(CountdownTimer(5));

                        }
                        else
                        {
                            landmarkMovingAverage.Reset();
                            // instructionText.text = "Please raise your brows";
                            timerText.text = "Please raise your brows for ";
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;

                    case CalibrationPhase.ReverseFrown:
                        if (nextCalibrationPhase != calibrationPhase)
                        {
                            // instructionText.text = "Get ready to frown";
                            timerText.text = "Get ready to frown in ";
                            StartCoroutine(CountdownTimer(5));
                        }
                        else
                        {
                            landmarkMovingAverage.Reset();
                            // instructionText.text = "Please do the reverse frown";
                            timerText.text = "Please do the reverse frown for ";
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;

                    case CalibrationPhase.End:
                        calibrationPhase = CalibrationPhase.End;
                        // instructionText.text = "Calibration Done!";
                        timerText.text = "Calibration Done!";
                        break;

                }
            }
        }



        public void ProcessFaceLandmarks(ARTrackablesChangedEventArgs<ARFace> trackablesChangedEventArgs)
        {
            if (!calibrationStarted)
            {
                return;
            }
            ARFace arFace = null;
            if (trackablesChangedEventArgs.added.Count > 0)
            {
                arFace = trackablesChangedEventArgs.added[0];
            }
            if (trackablesChangedEventArgs.updated.Count > 0)
            {
                arFace = trackablesChangedEventArgs.updated[0];
            }
            if (arFace != null && timerRunning && nextCalibrationPhase == calibrationPhase)
            {
                // process arFace
                var meshVertices = arFace.vertices;
                var smoothedVertices = landmarkMovingAverage.Process(meshVertices.ToArray());
                switch (calibrationPhase)
                {
                    case CalibrationPhase.Baseline:
                        CalibrationLandmarks.baselineLandmarks = smoothedVertices;
                        Debug.Log($"Set {CalibrationLandmarks.baselineLandmarks.Length} baseline landmarks");
                        break;

                    case CalibrationPhase.Smile:
                        CalibrationLandmarks.smileLandmarks = smoothedVertices;
                        Debug.Log($"Set {CalibrationLandmarks.smileLandmarks.Length} smile landmarks");
                        break;

                    case CalibrationPhase.EyebrowRaise:
                        CalibrationLandmarks.eyebrowraiseLandmarks = smoothedVertices;
                        Debug.Log($"Set {CalibrationLandmarks.eyebrowraiseLandmarks.Length} brow landmarks");
                        break;

                    case CalibrationPhase.ReverseFrown:
                        CalibrationLandmarks.reversefrownLandmarks = smoothedVertices;
                        Debug.Log($"Set {CalibrationLandmarks.reversefrownLandmarks.Length} frown landmarks");
                        break;

                    default:
                        break;

                }
            }
        }

        IEnumerator CountdownTimer(float startTime)
        {
            audioSource.Play();
            var currentTime = startTime;
            timerRunning = true;
            while (currentTime > 0)
            {
                UpdateTimerUI(currentTime);
                yield return new WaitForSeconds(1f);
                currentTime--;
            }

            // Ensure the timer stops at 0
            currentTime = 0;
            UpdateTimerUI(currentTime);

            // Call a method when the timer reaches 0
            TimerEnded();
        }

        void UpdateTimerUI(float time)
        {
            timerText.text += $"{time}s";
        }

        void TimerEnded()
        {
            Debug.Log($"CP : {calibrationPhase}; NCP : {nextCalibrationPhase} ");
            timerRunning = false;
            if (calibrationPhase == nextCalibrationPhase)
            {
                nextCalibrationPhase += 1;
            }
            else
            {
                calibrationPhase = nextCalibrationPhase;
            }
            // Add logic here (e.g., game over, restart, trigger event)
        }
    }
}