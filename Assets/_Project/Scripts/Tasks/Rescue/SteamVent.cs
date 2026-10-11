using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>A pressure-release vent on the coffee machine. Opens while powered (socket or pressure plate).</summary>
    public class SteamVent : MonoBehaviour, IPowerable
    {
        public CoffeeMachine machine;
        public int index;
        [Tooltip("Steam puff shown while open.")]
        public Transform steam;

        bool open;

        void Start() => SetPowered(false);

        public void SetPowered(bool powered)
        {
            open = powered;
            if (machine) machine.SetVent(index, powered);
            if (steam) steam.gameObject.SetActive(powered);
        }

        void Update()
        {
            if (!open || !steam) return;
            float s = 0.8f + Mathf.PingPong(Time.time * 3f, 0.4f);
            steam.localScale = new Vector3(s, s * 1.3f, s);
        }
    }
}
