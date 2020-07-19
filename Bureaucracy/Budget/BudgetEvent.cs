
using System;
using System.Linq;
using UnityEngine;

namespace Bureaucracy
{
    public class BudgetEvent : BureaucracyEvent
    {
        public readonly float MonthLength;
        public BudgetEvent(double budgetTime, BudgetManager manager, bool newKacAlarm)
        {
            MonthLength = SettingsClass.Instance.TimeBetweenBudgets;
            CompletionTime = budgetTime;
            Name = "Next Budget";
            ParentManager = manager;
            if(newKacAlarm) Utilities.NewKacAlarm("Next Budget", CompletionTime);
            StopTimewarpOnCompletion = true;
            AddTimer();
        }

        public override void OnEventCompleted()
        {
            Debug.Log("Bureaucracy]: OnBudgetAboutToFire");
            //Allows other Managers to do pre-budget work, as once the budget is done alot of stuff gets reset.
            InternalListeners.OnBudgetAboutToFire.Fire();
            // ReSharper disable once UnusedVariable
            RepDecay repDecay = new RepDecay();
            RepDecay.ApplyHardMode();
            double funding = Utilities.GetNetBudget("Budget");
            funding -= CrewManager.Instance.Bonuses(funding, true);
            double facilityDebt = Costs.GetFacilityMaintenanceCosts();
            double wageDebt = Math.Abs(funding + facilityDebt);
            if (funding < 0)
            {
                Debug.Log("[Bureaucracy]: Funding < 0. Paying debts");
                //pay wages first then facilities
                Utilities.PayWageDebt(wageDebt);
                Utilities.PayFacilityDebt(facilityDebt, wageDebt);
            }
            CrewManager.Instance.ProcessUnhappyCrew();
            if(SettingsClass.Instance.UseItOrLoseIt && funding > Funding.Instance.Funds) Funding.Instance.SetFunds(0.0d, TransactionReasons.Contracts);
            if(!SettingsClass.Instance.UseItOrLoseIt || Funding.Instance.Funds <= 0.0d || funding <= 0.0d) Funding.Instance.AddFunds(funding, TransactionReasons.Contracts);
            Debug.Log("[Bureaucracy]: OnBudgetAwarded. Awarding "+funding+" Costs: "+facilityDebt);
            InternalListeners.OnBudgetAwarded.Fire(funding, facilityDebt);
            Costs.Instance.ResetLaunchCosts();
            RepDecay.ApplyRepDecay(Bureaucracy.Instance.Settings.RepDecayPercent);
            for (int i = 0; i < Bureaucracy.Instance.RegisteredManagers.Count; i++)
            {
                Manager m = Bureaucracy.Instance.RegisteredManagers.ElementAt(i);
                m.ThisMonthsBudget = Utilities.GetNetBudget(m.Name);
            }
            InformParent();
        }
        
    }
}