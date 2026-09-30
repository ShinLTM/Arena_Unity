using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
         public float Speed = 5;
    }

    [System.Serializable]
    private class References
    {
         public CharacterController Controller;
    }


    [System.Serializable]
    public class StateContainer
    {
        public bool IsJumping = false;
    }

    const float MS_TO_KMH = 1 / 3.6f;

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;
    [SerializeField] private StateContainer _state;


    public StateContainer State => _state;
    void Update()
    {
        float t = Time.deltaTime;
        Move(t);
    }

    private void Move(float t)
    {
        transform.position += Vector3.forward * t / MS_TO_KMH * _settings.Speed;
    }
}
