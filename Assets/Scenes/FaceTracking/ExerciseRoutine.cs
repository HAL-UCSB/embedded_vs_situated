using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace UnityEngine.XR.ARFoundation.Samples
{

    public enum ExercisePhase
    {
        Start,
        Exercise,
        Break,
        End
    }
    public class ExerciseRoutine : MonoBehaviour
    {
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI timerText;

        private ExercisePhase exercisePhase;
        private ExercisePhase nextExercisePhase;
        private bool timerRunning = false;

        List<ExerciseType> exercises = new List<ExerciseType> { ExerciseType.kSmile, ExerciseType.kEyebrowRaise, ExerciseType.kReverseFrown };

        private int numRepetitions;
        private int numRep;
        private LandmarkMovingAverageFilter landmarkMovingAverage = new LandmarkMovingAverageFilter(10);
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Debug.Log("In ER Start");
            exercisePhase = ExercisePhase.Start;
            nextExercisePhase = ExercisePhase.Start;
            numRepetitions = exercises.Count * 2;
            numRep = 0;
        }

        // Update is called once per frame
        void Update()
        {
            // Debug.Log("In ER update");
            if (!timerRunning)
            {
                switch (nextExercisePhase)
                {
                    case ExercisePhase.Start:
                        instructionText.text = "Get ready";
                        nextExercisePhase = ExercisePhase.Exercise;
                        StartCoroutine(CountdownTimer(10));
                        break;
                    case ExercisePhase.Break:
                        if (nextExercisePhase != exercisePhase)
                        {
                            instructionText.text = "Break time";
                            StartCoroutine(CountdownTimer(5));
                            exercisePhase = ExercisePhase.Break;
                        }
                        break;
                    case ExercisePhase.Exercise:
                        if (nextExercisePhase != exercisePhase)
                        {
                            instructionText.text = $"Perform {exercises[numRep % exercises.Count]} exercise";
                            exercisePhase = ExercisePhase.Exercise;
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;

                    case ExercisePhase.End:
                        instructionText.text = "All done!";
                        break;


                }
            }
        }
        public ExerciseType currentExercise()
        {
            return exercises[numRep % exercises.Count];
        }
        public ExercisePhase currentExercisePhase()
        {
            return exercisePhase;
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
            Debug.Log($"EP : {exercisePhase}; NEP : {nextExercisePhase} ");
            timerRunning = false;
            if (exercisePhase == ExercisePhase.Exercise)
            { // check if all exercises are done
                numRep++;
                nextExercisePhase = numRep < numRepetitions ? ExercisePhase.Break : ExercisePhase.End;
            }
            else if (exercisePhase == ExercisePhase.Break)
            {
                nextExercisePhase = ExercisePhase.Exercise;
            }
            // Add logic here (e.g., game over, restart, trigger event)
        }
    }
}