using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ActivationBar : MonoBehaviour
    {
        public float maxActivation = 1f;
        private Image activationBarFill; // Assign the ActivationBarFill Image in the Inspector
        private float currentActivation;

        void Start()
        {
            currentActivation = 0.0f;
            activationBarFill = GetComponent<Image>();
            UpdateActivationBar();
        }

        public void SetActivation(float activationValue)
        {
            currentActivation = activationValue;
            currentActivation = Mathf.Clamp(currentActivation, 0, maxActivation);
            UpdateActivationBar();
        }

        private void UpdateActivationBar()
        {
            float activationPercentage = currentActivation / maxActivation;
            activationBarFill.fillAmount = activationPercentage;
            // Change color based on activation level
            if (activationPercentage > 0.7f)
                // activationBarFill.color = Color.blue; // High activation
                activationBarFill.color = new Color(0.902f, 0.380f, 0.004f, 1.0f); // High activation 
            else if (activationPercentage > 0.4f)
                // activationBarFill.color = Color.green;  // Mid activation
                activationBarFill.color = new Color(0.4f, 0.651f, 0.118f, 1.0f);  // Mid activation
            else
                // activationBarFill.color = Color.red;   // Low activation
                activationBarFill.color = new Color(0.271f, 0.459f, 0.706f, 1.0f);   // Low activation
        }
    }
}