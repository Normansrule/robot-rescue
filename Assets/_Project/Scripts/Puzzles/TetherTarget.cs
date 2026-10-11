using System.Collections.Generic;
using RobotRescue.Bots;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    public enum TetherMode
    {
        /// <summary>Plug in: the bot powers the target. The cable has a fixed length leash.</summary>
        Power,
        /// <summary>Hook a loose object and reel it to the bot, which catches it.</summary>
        Pull,
        /// <summary>Hook a fixed anchor and swing on it. Reel to shorten/lengthen.</summary>
        Swing,
    }

    /// <summary>
    /// Anything the Tether Cable can attach to. Keeps a registry so the cable can do
    /// gamepad-friendly aim assist without physics queries.
    /// </summary>
    public abstract class TetherTarget : MonoBehaviour
    {
        public static readonly List<TetherTarget> All = new List<TetherTarget>();

        [Tooltip("Where the cable plugs/hooks in. Defaults to this transform.")]
        public Transform attachPoint;

        public abstract TetherMode Mode { get; }
        public virtual bool CanAttach => isActiveAndEnabled;
        public Vector3 AttachPosition => attachPoint ? attachPoint.position : transform.position;
        public TetherCable AttachedCable { get; private set; }

        protected virtual void OnEnable() => All.Add(this);

        protected virtual void OnDisable()
        {
            All.Remove(this);
            if (AttachedCable) AttachedCable.Detach();
        }

        public virtual void OnTetherAttached(TetherCable cable) => AttachedCable = cable;
        public virtual void OnTetherDetached(TetherCable cable) { if (AttachedCable == cable) AttachedCable = null; }

        /// <summary>Short verb for the HUD prompt, e.g. "Plug in".</summary>
        public virtual string PromptVerb => Mode switch
        {
            TetherMode.Power => "Plug in",
            TetherMode.Pull => "Pull",
            _ => "Swing",
        };
    }
}
