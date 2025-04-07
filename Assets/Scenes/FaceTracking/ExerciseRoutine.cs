using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Assertions;

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
        // public TextMeshProUGUI instructionText;
        public TextMeshProUGUI timerText;

        string instructionText;
        private ExercisePhase exercisePhase;
        private ExercisePhase nextExercisePhase;
        private bool timerRunning = false;

        List<ExerciseType> exerciseTypes = new List<ExerciseType> { ExerciseType.kSmile, ExerciseType.kEyebrowRaise, ExerciseType.kReverseFrown };
        // List<string> exercises = new List<string> { "smile", "raise eyebrows", "frown" };

        List<List<ExerciseType>> exerciseTypesPermutation = new List<List<ExerciseType>>{
            new List<ExerciseType> { ExerciseType.kSmile, ExerciseType.kEyebrowRaise, ExerciseType.kReverseFrown},
            new List<ExerciseType> { ExerciseType.kSmile, ExerciseType.kReverseFrown, ExerciseType.kEyebrowRaise},
            new List<ExerciseType> { ExerciseType.kEyebrowRaise, ExerciseType.kSmile, ExerciseType.kReverseFrown},
            new List<ExerciseType> { ExerciseType.kEyebrowRaise, ExerciseType.kReverseFrown, ExerciseType.kSmile},
            new List<ExerciseType> { ExerciseType.kReverseFrown, ExerciseType.kSmile, ExerciseType.kEyebrowRaise},
            new List<ExerciseType> { ExerciseType.kReverseFrown, ExerciseType.kEyebrowRaise, ExerciseType.kSmile},
        };
        List<List<string>> exercisePermutation = new List<List<string>> {
            new List<string> { "smile", "raise eyebrows", "frown"},
            new List<string> { "smile", "frown", "raise eyebrows"},
            new List<string> { "raise eyebrows", "smile", "frown"},
            new List<string> { "raise eyebrows", "frown", "smile"},
            new List<string> { "frown", "smile", "raise eyebrows"},
            new List<string> { "frown", "raise eyebrows", "smile"},
        };

        private int numRepetitions;
        private int numExercises;
        private int currRep;
        private int currExercise;

        AudioSource audioSource;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Debug.Log("In ER Start");
            exercisePhase = ExercisePhase.Start;
            nextExercisePhase = ExercisePhase.Start;
            numRepetitions = 6; //exercises.Count * 2;
            numExercises = 3;
            currRep = 1;
            currExercise = 0;
            audioSource = GetComponent<AudioSource>();

            Assert.IsNotNull(timerText);
            Assert.IsNotNull(audioSource);
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log($"Current status : {currRep}, {currExercise}");
            if (!timerRunning)
            {
                switch (nextExercisePhase)
                {
                    case ExercisePhase.Start:
                        // instructionText = "Get ready";
                        instructionText = $"Get ready to {exercisePermutation[currRep - 1][currExercise]} in ";
                        nextExercisePhase = ExercisePhase.Exercise;
                        StartCoroutine(CountdownTimer(10));
                        break;
                    case ExercisePhase.Break:
                        if (nextExercisePhase != exercisePhase)
                        {
                            // instructionText.text = "Break time";
                            instructionText = "Break time for ";
                            StartCoroutine(CountdownTimer(5));
                            exercisePhase = ExercisePhase.Break;
                        }
                        break;
                    case ExercisePhase.Exercise:
                        if (nextExercisePhase != exercisePhase)
                        {
                            // instructionText.text = $"Perform {exercises[numRep % exercises.Count]} exercise";
                            instructionText = $"Please {exercisePermutation[currRep - 1][currExercise]} for ";
                            exercisePhase = ExercisePhase.Exercise;
                            StartCoroutine(CountdownTimer(10));
                        }
                        break;

                    case ExercisePhase.End:
                        // instructionText.text = "All done!";
                        timerText.text = "All done!";
                        exercisePhase = ExercisePhase.End;
                        break;


                }
            }
        }
        public ExerciseType currentExercise()
        {
            // return exerciseTypes[currExercise];
            return exerciseTypesPermutation[currRep - 1][currExercise];
        }
        public ExercisePhase currentExercisePhase()
        {
            return exercisePhase;
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
            timerText.text = instructionText + $"{time}s";
        }

        void TimerEnded()
        {
            Debug.Log($"EP : {exercisePhase}; NEP : {nextExercisePhase} ");
            timerRunning = false;
            if (exercisePhase == ExercisePhase.Exercise)
            { // check if all exercises are done
                currExercise++;
                if (currExercise == numExercises)
                {
                    currExercise = 0;
                    currRep++;
                }
                nextExercisePhase = currRep <= numRepetitions ? ExercisePhase.Break : ExercisePhase.End;
            }
            else if (exercisePhase == ExercisePhase.Break)
            {
                nextExercisePhase = ExercisePhase.Exercise;
            }
            // Add logic here (e.g., game over, restart, trigger event)
        }
    }
}