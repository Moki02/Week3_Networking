using Fusion;
using UnityEngine;

namespace Com.MyCompany.MyGame
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 direction;
        public NetworkButtons buttons;
        public float rotationY;
    }

    public enum InputButtons
    {
        Jump,
        Crouch
    }
}
