﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class BureaucracySpaceCentre : Bureaucracy
    {
        
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BureaucracyFlight : Bureaucracy
    {
        
    }
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class BureaucracyTrackingStation : Bureaucracy
    {
        
    }
    public class Bureaucracy : MonoBehaviour
    {
        [UsedImplicitly] private Utilities utilitiesReference = new Utilities();
        public SettingsClass Settings;
        public static Bureaucracy Instance;
        public List<Manager> RegisteredManagers = new List<Manager>();
        public bool ExistingSave;
        [UsedImplicitly] public ManagerProgressEvent ProgressEvent;
        public double LastProgressUpdate;

        private void Awake()
        {
            //Mod starts here.
            Settings = new SettingsClass();
            RegisterBureaucracyManagers();
            Instance = this;
            LastProgressUpdate = Planetarium.GetUniversalTime();
            Debug.Log("[Bureaucracy]: Awake");
        }

        private void Start()
        {
            InternalListeners.OnBudgetAwarded.Add(GeneratePostBudgetReport);
            KacWrapper.InitKacWrapper();
            if (SettingsClass.Instance.KctError && Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalConstructionTime")) UiController.Instance.ErrorWindow = UiController.KctError();
        }
        
        private void RegisterBureaucracyManagers()
        {
            //Register internal manager classes (and gives me a place to store the references). Expandable.
            RegisteredManagers.Add(new BudgetManager());
            RegisteredManagers.Add(new FacilityManager());
            RegisteredManagers.Add(new ResearchManager());
            RegisteredManagers.Add(new CrewManager(HighLogic.CurrentGame.CrewRoster.Crew.ToList()));
        }

        public void RetryKacAlarm()
        {
            //KAC API isn't always ready when we try to add an alarm, so we retry after a few seconds.
            if (!KacWrapper.AssemblyExists)
            {
                Debug.Log("[Bureaucracy]: Still can't find KAC. Assuming it's not installed");
                return;
            }
            KacWrapper.Kacapi.KacAlarmList kacAlarms = KacWrapper.Kac.Alarms;
            for (int i = 0; i < kacAlarms.Count; i++)
            {
                KacWrapper.Kacapi.KacAlarm alarm = kacAlarms.ElementAt(i);
                if (alarm.Name != "Next Budget") continue;
                Debug.Log("[Bureaucracy]: KAC Alarm already exists.");
                return;
            }
            double alarmTime = Planetarium.GetUniversalTime() + SettingsClass.Instance.TimeBetweenBudgets * FlightGlobals.GetHomeBody().solarDayLength;
            Utilities.NewKacAlarm("Next Budget", alarmTime);
            Debug.Log("[Bureaucracy]: Created new KAC Alarm at "+alarmTime);
        }

        public void SetCalcsDirty()
        {
            //Costs gets called alot at budget time, to save overheads we cache them for 5 seconds.
            //Setting Dirty lets mod know that it needs to calculate them again.
            Costs.Instance.SetCalcsDirty();
        }
        public void RegisterManager(Manager m)
        {
            //For mods to register custom managers.
            if (RegisteredManagers.Contains(m))
            {
                Debug.Log("[Bureaucracy]: Attempted to register manager" +m.Name+ " but already exists");
                return;
            }

            Debug.Log("[Bureaucracy]: Registered Custom Manager" +m.Name);
            RegisteredManagers.Add(m);
        }

        public void OnLoad(ConfigNode node)
        {
            //ScenarioModule OnLoad event redirects here, each class handles it's own saving/loading (for better encapsulation).
            Debug.Log("[Bureaucracy]: OnLoad");
            SettingsClass.Instance.InGameLoad();
            BudgetManager.Instance.OnLoad(node);
            FacilityManager.Instance.OnLoad(node);
            ResearchManager.Instance.OnLoad(node);
            CrewManager.Instance.OnLoad(node);
            RandomEventLoader.Instance.OnLoad(node);
            UiController.Instance.OnLoad(node);
            node.TryGetValue("existingSave", ref ExistingSave);
            node.TryGetValue("lastProgressUpdate", ref LastProgressUpdate);
            ProgressEvent ??= new ManagerProgressEvent();
            Debug.Log("[Bureaucracy]: OnLoad Complete");
        }

        public void OnSave(ConfigNode node)
        {
            //ScenarioModule OnLoad event redirects here, each class handles it's own saving/loading (for better encapsulation).
            Debug.Log("[Bureaucracy]: OnSave");
            SettingsClass.Instance.InGameSave();
            BudgetManager.Instance.OnSave(node);
            FacilityManager.Instance.OnSave(node);
            ResearchManager.Instance.OnSave(node);
            CrewManager.Instance.OnSave(node);
            RandomEventLoader.Instance.OnSave(node);
            UiController.Instance.OnSave(node);
            node.SetValue("existingSave", ExistingSave, true);
            node.SetValue("lastProgressUpdate", LastProgressUpdate, true);
            Debug.Log("[Bureaucracy]: OnSave Complete");
        }

        private void GeneratePostBudgetReport(double data0, double data1)
        {
            //All Managers generate a report.
            Debug.Log("[Bureaucracy]: Firing Manager Reports");
            for (int i = 0; i < RegisteredManagers.Count; i++)
            {
                Manager m = RegisteredManagers.ElementAt(i);
                m.MakeReport();
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < RegisteredManagers.Count; i++)
            {
                Manager m = RegisteredManagers.ElementAt(i);
                m.UnregisterEvents();
            }
            InternalListeners.OnBudgetAwarded.Remove(GeneratePostBudgetReport);
        }
    }
}