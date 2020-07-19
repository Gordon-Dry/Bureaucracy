using System.Linq;

namespace Bureaucracy
{
    public class ManagerProgressEvent : BureaucracyEvent
    {
        public ManagerProgressEvent()
        {
            CompletionTime = Planetarium.GetUniversalTime() + FlightGlobals.GetHomeBody().solarDayLength;
            AddTimer();
        }

        public override void OnEventCompleted()
        {
            for (int i = 0; i < Bureaucracy.Instance.RegisteredManagers.Count; i++)
            {
                Manager m = Bureaucracy.Instance.RegisteredManagers.ElementAt(i);
                m.ProgressTask();
            }
            Bureaucracy.Instance.ProgressEvent = new ManagerProgressEvent();
            Bureaucracy.Instance.LastProgressUpdate = Planetarium.GetUniversalTime();
        }
    }
}