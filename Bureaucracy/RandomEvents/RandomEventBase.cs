using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Bureaucracy
{
    public abstract class RandomEventBase
    {
        [UsedImplicitly] private PopupDialog eventDialog;
        public string Name;
        protected string Title;
        protected string Body;
        protected string AcceptString;
        private string declineString;
        protected bool CanBeDeclined;
        protected float EventEffect;
        protected string KerbalName;
        private string bodyName;

        public abstract bool EventCanFire();

        protected void LoadConfig(ConfigNode cn)
        {
            if(!cn.TryGetValue("Name", ref Name)) throw new ArgumentException("Event is missing a Name!");
            if(!cn.TryGetValue("Title", ref Title)) throw new ArgumentException(Name+" has no title!");
            if(!cn.TryGetValue("Body", ref Body)) throw new ArgumentException(Name+" has no body!");
            if(!cn.TryGetValue("AcceptButtonText", ref AcceptString)) throw new ArgumentException(Name + " missing AcceptButtonText");
            if(cn.TryGetValue("canBeDeclined", ref CanBeDeclined) && !cn.TryGetValue("DeclineButtonText", ref declineString)) throw new ArgumentException(Name + "Can be declined but DeclineButtonText not set");
            if(!cn.TryGetValue("Effect", ref EventEffect)) throw new ArgumentException(Name+" has no Effect defined in cfg");
            ReplaceStrings();
        }

        private void ReplaceStrings()
        {
            bodyName = Utilities.GetARandomBody();
            KerbalName = Utilities.Instance.GetARandomKerbal();
            Name = Name.Replace("<kerbal>", KerbalName);
            Name = Name.Replace("<body>", bodyName);
            Title = Title.Replace("<kerbal>", KerbalName);
            Title = Title.Replace("<body>", bodyName);
            Body = Body.Replace("<kerbal>", KerbalName);
            Body = Body.Replace("<body>", bodyName);
            AcceptString = AcceptString.Replace("<kerbal>", KerbalName);
            AcceptString = AcceptString.Replace("<body>", bodyName);
            declineString = declineString?.Replace("<kerbal>", KerbalName);
            declineString = declineString?.Replace("<body>", bodyName);
        }

        protected abstract void OnEventAccepted();

        protected abstract void OnEventDeclined();

        public void OnEventFire()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            innerElements.Add(new DialogGUISpace(10));
            innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(Body)));
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(new DialogGUIScrollList(-Vector2.one, false, false, vertical));
            dialogElements.Add(new DialogGUIButton(AcceptString, OnEventAccepted));
            if(CanBeDeclined) dialogElements.Add(new DialogGUIButton(declineString, OnEventDeclined));
            eventDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("EventDialog", "", Title, UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 300, 200), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }

        private static DialogGUIBase[] PaddedLabel(string stringToPad)
        {
            DialogGUIBase[] paddedLayout = new DialogGUIBase[2];
            paddedLayout[0] = new DialogGUISpace(10);
            paddedLayout[1] = new DialogGUILabel(stringToPad);
            return paddedLayout;
        }

    }
}