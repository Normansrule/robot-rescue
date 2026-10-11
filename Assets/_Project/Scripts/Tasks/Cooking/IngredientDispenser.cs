using System.Collections.Generic;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>
    /// Endless supply crate. Keeps a fresh copy of its (inactive) template ingredient sitting at the spawn point.
    /// </summary>
    public class IngredientDispenser : MonoBehaviour
    {
        [Tooltip("An inactive ingredient to copy.")]
        public GameObject template;
        public Transform spawnPoint;
        public float cooldown = 1.5f;
        [Tooltip("Max copies alive at once, so the kitchen doesn't fill with tomatoes.")]
        public int maxAlive = 4;
        public float clearRadius = 0.5f;

        readonly List<GameObject> alive = new List<GameObject>();
        float timer;

        void Update()
        {
            alive.RemoveAll(g => g == null || !g.activeInHierarchy);
            timer -= Time.deltaTime;
            if (timer > 0f || alive.Count >= maxAlive || !template || !spawnPoint) return;

            foreach (var g in alive)
                if ((g.transform.position - spawnPoint.position).sqrMagnitude < clearRadius * clearRadius) return;

            var copy = Instantiate(template, spawnPoint.position, spawnPoint.rotation, transform);
            copy.AddComponent<DispensedMarker>().source = this;
            copy.SetActive(true);
            alive.Add(copy);
            timer = cooldown;
        }
    }
}
