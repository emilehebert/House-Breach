using UnityEngine;

public class SkyboxManager : MonoBehaviour
{
    //[SerializeField] private Transform playerFollow;
    [SerializeField] private float rotateSpeed = 10f;    // degrees per second

    void LateUpdate()
    {
        // Keep the skybox centred on the player
        //transform.position = playerFollow.position;

        // Spin it around its up (Y) axis
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);
    }
}
