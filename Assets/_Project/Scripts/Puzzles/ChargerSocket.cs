using RobotRescue.Bots;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// A wall charger: plug the Tether in to charge PIP instead of powering something. Still passes power on to any
    /// linked objects, so a puzzle can make you choose between charging and holding a door open.
    /// </summary>
    public class ChargerSocket : PowerSocket, IChargeSource
    {
        public float chargePerSecond = 0.15f;
        public string SourceName => "Charger";

        BotBattery battery;

        public float ChargeRate(BotBattery b) => b == battery && b.Charge01 < 1f ? chargePerSecond : 0f;

        public override string PromptVerb => "Plug in to charge";

        public override void OnTetherAttached(TetherCable cable)
        {
            base.OnTetherAttached(cable);
            battery = cable.GetComponent<BotBattery>();
            if (battery)
            {
                battery.AddSource(this);
                battery.LastChargerPosition = cable.transform.position;
            }
        }

        public override void OnTetherDetached(TetherCable cable)
        {
            base.OnTetherDetached(cable);
            if (battery) battery.RemoveSource(this);
            battery = null;
        }
    }
}
