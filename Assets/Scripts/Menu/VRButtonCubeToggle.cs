using UnityEngine;

public class VRButtonCubeToggle : MonoBehaviour
{
    [SerializeField] private GameObject targetCube;

    public void ToggleCube()
    {
        if (targetCube != null)
        {
            targetCube.SetActive(!targetCube.activeSelf);
        }
    }

    public void SetTargetCube(GameObject cube)
    {
        targetCube = cube;
    }
}
