using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class LandmarkMovingAverageFilter
    {
        private readonly int _windowSize;
        private readonly Queue<Vector3[]> _landmarkHistory;
        private Vector3[] _runningSum;
        private bool _initialized;

        public bool IsInitialized() => _initialized;

        /// <summary>
        /// Initializes a moving average filter for face landmarks.
        /// </summary>
        /// <param name="windowSize">Number of frames to average over</param>
        public LandmarkMovingAverageFilter(int windowSize = 5)
        {
            _windowSize = windowSize;
            _landmarkHistory = new Queue<Vector3[]>(windowSize);
            _runningSum = new Vector3[468]; // ARCore provides 468 landmarks
            _initialized = false;
        }

        /// <summary>
        /// Processes a new frame of landmarks and returns the smoothed landmarks.
        /// </summary>
        /// <param name="currentLandmarks">Array of 468 Vector3 landmarks</param>
        /// <returns>Smoothed array of 468 Vector3 landmarks</returns>
        public Vector3[] Process(Vector3[] currentLandmarks)
        {
            if (currentLandmarks == null || currentLandmarks.Length != 468)
            {
                Debug.LogError("Input must be an array of exactly 468 landmarks");
                return currentLandmarks;
            }

            if (!_initialized)
            {
                Initialize(currentLandmarks);
                return currentLandmarks; // Return first frame as-is
            }

            // Add current landmarks to history
            _landmarkHistory.Enqueue(currentLandmarks);

            // Add to running sum
            for (int i = 0; i < 468; i++)
            {
                _runningSum[i] += currentLandmarks[i];
            }

            // If we've exceeded window size, remove the oldest entry
            if (_landmarkHistory.Count > _windowSize)
            {
                var oldest = _landmarkHistory.Dequeue();
                for (int i = 0; i < 468; i++)
                {
                    _runningSum[i] -= oldest[i];
                }
            }

            // Calculate average
            var averagedLandmarks = new Vector3[468];
            float divisor = _landmarkHistory.Count;
            for (int i = 0; i < 468; i++)
            {
                averagedLandmarks[i] = _runningSum[i] / divisor;
            }

            return averagedLandmarks;
        }

        /// <summary>
        /// Resets the filter history.
        /// </summary>
        public void Reset()
        {
            _landmarkHistory.Clear();
            _runningSum = new Vector3[468];
            _initialized = false;
        }

        private void Initialize(Vector3[] initialLandmarks)
        {
            // Make a copy of the initial landmarks
            var copy = new Vector3[468];
            System.Array.Copy(initialLandmarks, copy, 468);
            _landmarkHistory.Enqueue(copy);

            // Initialize running sum
            for (int i = 0; i < 468; i++)
            {
                _runningSum[i] = copy[i];
            }

            _initialized = true;
        }
    }
}