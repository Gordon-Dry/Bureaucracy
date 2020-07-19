using System.Collections.Generic;
using System.Linq;
using Contracts;
using KSP.UI.Screens;
using UnityEngine;
using FlightTracker;
// ReSharper disable MemberCanBeMadeStatic.Local

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class ExternalListeners : MonoBehaviour
    {
        private KerbalismApi kerbalism;
        private EventData<List<ScienceSubject>, List<double>> onKerbalismScience;

        private void Awake()
        {
            Debug.Log("[Bureaucracy]: Waking GameEvents");
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            Debug.Log("[Bureaucracy]: Registering Events");
            GameEvents.OnVesselRollout.Add(AddLaunch);
            GameEvents.Contract.onOffered.Add(OnContractOffered);
            GameEvents.onFacilityContextMenuSpawn.Add(OnFacilityContextMenuSpawn);
            GameEvents.OnScienceRecieved.Add(OnScienceReceived);
            GameEvents.OnCrewmemberHired.Add(OnCrewMemberHired);
            GameEvents.onKerbalStatusChanged.Add(PotentialKerbalDeath);
            GameEvents.onGUIAstronautComplexSpawn.Add(AstronautComplexSpawned);
            GameEvents.onGUIAstronautComplexDespawn.Add(AstronautComplexDespawned);
            Debug.Log("[Bureaucracy]: Stock Events Registered");
            FlightTrackerApi.OnFlightTrackerUpdated.Add(HandleRecovery);
            Debug.Log("[Bureaucracy]: FlightTracker Events Registered");
            kerbalism = new KerbalismApi();
            if (SettingsClass.Instance.HandleScience && kerbalism.ActivateKerbalismInterface())
            {
                onKerbalismScience = GameEvents.FindEvent<EventData<List<ScienceSubject>, List<double>>>("onSubjectsReceived");
                if (onKerbalismScience == null) return;
                onKerbalismScience.Add(OnKerbalismScienceReceived);
                Debug.Log("[Bureaucracy]: Kerbalism Event Registered");
            }
            Debug.Log("[Bureaucracy]: All Events Successfully Registered");
        }

        private static void AstronautComplexDespawned()
        {
            AstronautComplexOverride.Instance.AstronautComplexSpawned = false;
        }

        private void AstronautComplexSpawned()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            AstronautComplexOverride.Instance.UpdateCount = 4;
            AstronautComplexOverride.Instance.AstronautComplexSpawned = true;
        }

        private void PotentialKerbalDeath(ProtoCrewMember crewMember, ProtoCrewMember.RosterStatus statusFrom, ProtoCrewMember.RosterStatus statusTo)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            //No need to check if the Kerbal hasn't died. Kerbals always go missing before they go dead, so just check for Missing.
            if (statusTo != ProtoCrewMember.RosterStatus.Missing) return;
            CrewManager.Instance.ProcessDeadKerbal(crewMember);
        }

        private void OnCrewMemberHired(ProtoCrewMember crewMember, int numberOfActiveKerbals)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            if (FacilityManager.Instance.GetFacilityByName("AstronautComplex").IsClosed)
            {
                Funding.Instance.AddFunds(GameVariables.Instance.GetRecruitHireCost(numberOfActiveKerbals - 1), TransactionReasons.CrewRecruited);
                UiController.Instance.ErrorWindow = UiController.NoHireWindow();
                HighLogic.CurrentGame.CrewRoster.Remove(crewMember);
                return;
            }
            CrewManager.Instance.AddNewCrewMember(crewMember);
            KerbalRoster.SetExperienceLevel(crewMember, 1);
        }

        private void HandleRecovery(ProtoCrewMember crewMember)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            CrewManager.Instance.UpdateCrewBonus(crewMember, FlightTrackerApi.Instance.GetLaunchTime(crewMember.name));
            if (!SettingsClass.Instance.RetirementEnabled) return;
            CrewManager.Instance.ExtendRetirement(crewMember, FlightTrackerApi.Instance.GetLaunchTime(crewMember.name));
            CrewManager.Instance.ProcessRetirees();
        }

        private void OnScienceReceived(float science, ScienceSubject subject, ProtoVessel protoVessel, bool reverseEngineered)
        {
            if (!SettingsClass.Instance.HandleScience) return;
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            if (science < 0.1f) return;
            ResearchAndDevelopment.Instance.AddScience(-science, TransactionReasons.ScienceTransmission);
            ResearchManager.Instance.NewScienceReceived(science, subject);
        }

        private void OnKerbalismScienceReceived(List<ScienceSubject> subjects, List<double> data)
        {
            if (!SettingsClass.Instance.HandleScience) return;
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            for (int i = 0; i < subjects.Count; i++)
            {
                ResearchManager.Instance.NewScienceReceived((float)data.ElementAt(i), subjects.ElementAt(i));
            }
        }

        private void OnFacilityContextMenuSpawn(KSCFacilityContextMenu menu)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            FacilityMenuOverride.Instance.FacilityMenuSpawned(menu);
        }


        private void OnContractOffered(Contract contract)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            ContractInterceptor.Instance.OnContractOffered(contract);
        }

        private void AddLaunch(ShipConstruct ship)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
            Costs.Instance.AddLaunch(ship);
            string editor = ship.shipFacility == EditorFacility.VAB ? "VehicleAssemblyBuilding" : "SpaceplaneHangar";
            BureaucracyFacility bf = FacilityManager.Instance.GetFacilityByName(editor);
            if (!bf.IsClosed) return;
            bf.LaunchesThisMonth++;
            if (bf.LaunchesThisMonth > 2) Utilities.SabotageLaunch();

        }

        private void OnDisable()
        {
            Debug.Log("[Bureaucracy]: Unregistering Events");
            GameEvents.OnVesselRollout.Remove(AddLaunch);
            GameEvents.Contract.onOffered.Remove(OnContractOffered);
            GameEvents.onFacilityContextMenuSpawn.Remove(OnFacilityContextMenuSpawn);
            GameEvents.OnScienceRecieved.Remove(OnScienceReceived);
            GameEvents.OnCrewmemberHired.Remove(OnCrewMemberHired);
            GameEvents.onKerbalStatusChanged.Remove(PotentialKerbalDeath);
            GameEvents.onGUIAstronautComplexSpawn.Remove(AstronautComplexSpawned);
            GameEvents.onGUIAstronautComplexDespawn.Remove(AstronautComplexDespawned);
            Debug.Log("[Bureaucracy] Unregistered Stock Events");
            FlightTrackerApi.OnFlightTrackerUpdated.Remove(HandleRecovery);
            Debug.Log("[Bureaucracy] Unregistered Flight Tracker Event");
            if (onKerbalismScience == null) return;
            onKerbalismScience.Remove(OnKerbalismScienceReceived);
            Debug.Log("[Bureaucracy]: Unregistered Kerbalism Event");
        }
    }
}