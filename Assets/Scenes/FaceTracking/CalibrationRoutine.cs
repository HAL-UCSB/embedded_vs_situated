using System.Collections;
using TMPro;

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

        private CalibrationPhase calibrationPhase;
        private CalibrationPhase nextCalibrationPhase;
        private bool timerRunning = false;

        private LandmarkMovingAverageFilter landmarkMovingAverage = new LandmarkMovingAverageFilter(10);
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            calibrationPhase = CalibrationPhase.Baseline;
            nextCalibrationPhase = CalibrationPhase.Baseline;
        }

        // Update is called once per frame
        void Update()
        {
            if (calibrationPhase == CalibrationPhase.End)
            {
                SceneManagement.SceneManager.LoadScene(1);
            }
            if (!timerRunning)
            {
                switch (nextCalibrationPhase)
                {
                    case CalibrationPhase.Baseline:
                        instructionText.text = "Please keep a neutral expression";
                        StartCoroutine(CountdownTimer(10));
                        break;
                    case CalibrationPhase.Smile:
                        if (nextCalibrationPhase != calibrationPhase)
                        {
                            instructionText.text = "Get ready to smile";
                            // calibrationPhase = CalibrationPhase.Smile;
                            StartCoroutine(CountdownTimer(5));
                        }
                        else
                        {
                            landmarkMovingAverage.Reset();
                            instructionText.text = "Please smile";
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;
                    case CalibrationPhase.EyebrowRaise:
                        if (nextCalibrationPhase != calibrationPhase)
                        {
                            instructionText.text = "Get ready to raise brows";
                            // calibrationPhase = CalibrationPhase.EyebrowRaise;
                            StartCoroutine(CountdownTimer(5));

                        }
                        else
                        {
                            landmarkMovingAverage.Reset();
                            instructionText.text = "Please raise your brows";
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;

                    case CalibrationPhase.ReverseFrown:
                        if (nextCalibrationPhase != calibrationPhase)
                        {
                            instructionText.text = "Get ready to frown";
                            StartCoroutine(CountdownTimer(5));
                        }
                        else
                        {
                            landmarkMovingAverage.Reset();
                            instructionText.text = "Please do the reverse frown";
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;

                    case CalibrationPhase.End:
                        calibrationPhase = CalibrationPhase.End;
                        instructionText.text = "Calibration Done!";
                        break;

                }
            }
        }



        public void ProcessFaceLandmarks(ARTrackablesChangedEventArgs<ARFace> trackablesChangedEventArgs)
        {
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
                        Debug.LogFormat($"Set {CalibrationLandmarks.baselineLandmarks.Length} baseline landmarks", tag = "IntARFace");
                        break;

                    case CalibrationPhase.Smile:
                        CalibrationLandmarks.smileLandmarks = smoothedVertices;
                        Debug.LogFormat($"Set {CalibrationLandmarks.smileLandmarks.Length} smile landmarks", tag = "IntARFace");
                        break;

                    case CalibrationPhase.EyebrowRaise:
                        CalibrationLandmarks.eyebrowraiseLandmarks = smoothedVertices;
                        Debug.LogFormat($"Set {CalibrationLandmarks.eyebrowraiseLandmarks.Length} brow landmarks", tag = "IntARFace");
                        break;

                    case CalibrationPhase.ReverseFrown:
                        CalibrationLandmarks.reversefrownLandmarks = smoothedVertices;
                        Debug.LogFormat($"Set {CalibrationLandmarks.reversefrownLandmarks.Length} frown landmarks", tag = "IntARFace");
                        break;

                    default:
                        break;

                }
            }
        }

        IEnumerator CountdownTimer(float startTime)
        {
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
            if (timerText != null)
                timerText.text = $"Time: {time}s";
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