using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI;
using UnityEngine;

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AstronautComplexOverride : MonoBehaviour
    {
        public bool AstronautComplexSpawned;
        public static AstronautComplexOverride Instance;
        public int UpdateCount = 4;

        private void Awake()
        {
            Instance = this;
        }

        private void LateUpdate()
        {
            if (!AstronautComplexSpawned || UpdateCount <= 0) return;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            List<CrewListItem> crewItems = FindObjectsOfType<CrewListItem>().ToList();
            UpdateCount--;
            for (int i = 0; i < crewItems.Count; i++)
            {
                CrewListItem c = crewItems.ElementAt(i);
                if (c.GetCrewRef().type != ProtoCrewMember.KerbalType.Crew) continue;
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                c.SetLabel(GenerateAstronautString(c.GetCrewRef().name));
            }
        }

        private static string GenerateAstronautString(string kerbalName)
        {
            CrewMember c = CrewManager.Instance.Kerbals[kerbalName];
            //if for whatever reason we can't find the CrewMember just leave it at default
            if (c == null) return "Available For Next Mission";
            StringBuilder sb = new StringBuilder();
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            if (c.CrewReference().inactive) sb.AppendLine( "In Training | " + "Wage: " + c.Wage);
            else
            {
                float morale = (1 - (float) c.UnhappinessEvents.Count / c.MaxStrikes) * 100;
                if (float.IsNaN(morale)) morale = 100;
                if (float.IsNegativeInfinity(morale)) morale = 0;
                sb.AppendLine("Morale: " + Math.Round(morale, 0) + "% | Wage: " + c.Wage);
            }

            if (!SettingsClass.Instance.RetirementEnabled) return sb.ToString();
            KeyValuePair<int, string> retirementDate = Utilities.ConvertUtToRealTime(c.RetirementDate - Planetarium.GetUniversalTime());
            sb.AppendLine("Retires in " + retirementDate.Key + " " + retirementDate.Value);

            return sb.ToString();
        }
    }
}