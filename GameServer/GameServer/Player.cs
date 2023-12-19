using System.Numerics;

namespace GameServer
{
    public class Player
    {
        public int Id;
        public string Username;

        public Vector3 Position;
        public Quaternion Rotation;
        
        private float _moveSpeed = 5f / Constants.TicksPerSec;
        private bool[] _inputs;

        public Player(int id, string username, Vector3 spawnPosition)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = Quaternion.Identity;
            _inputs = new bool[4];
        }
        public void Update()
        {
            Vector2 inputDirection = Vector2.Zero;
            if (_inputs[0])
            {
                inputDirection.Y += 1;
            }
            if (_inputs[1])
            {
                inputDirection.Y -= 1;
            }
            if (_inputs[2])
            {
                inputDirection.X += 1;
            }
            if (_inputs[3])
            {
                inputDirection.X -= 1;
            }

            Move(inputDirection);
        }

        private void Move(Vector2 inputDirection)
        {
            Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), Rotation);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

            Vector3 moveDirection = right * inputDirection.X + forward * inputDirection.Y;
            Position += moveDirection * _moveSpeed;

            ServerSend.PlayerPosition(this);
            ServerSend.PlayerRotation(this);
        }
        public void SetInput(bool[] inputs, Quaternion rotation)
        {
            _inputs = inputs;
            Rotation = rotation;
        }
    }
    
}