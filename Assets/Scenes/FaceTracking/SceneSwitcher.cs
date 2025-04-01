using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void MannequinScene()
    {
        SceneManager.LoadScene("FaceMeshMannequin");
    }

    public void BarChartScene()
    {
        SceneManager.LoadScene("FaceMeshBarChart");
    }

    public void SelfieScene()
    {
        SceneManager.LoadScene("FaceMesh");
    }
    // Update is called once per frame
    void Update()
    {

    }
}
