namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Controls;
    using EventTraceKit.VsExtension.Windows;

    public class DropDownTextBoxAutomationPeer
        : FrameworkElementAutomationPeer, IValueProvider, IExpandCollapseProvider
    {
        public DropDownTextBoxAutomationPeer(DropDownTextBox owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string GetClassNameCore()
        {
            return nameof(DropDownTextBox);
        }

        public override object GetPattern(PatternInterface pattern)
        {
            switch (pattern) {
                case PatternInterface.Value:
                case PatternInterface.ExpandCollapse:
                    return this;
                default:
                    return base.GetPattern(pattern);
            }
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();

            TextBox textBox = ((DropDownTextBox)Owner).TextBoxSite;
            if (textBox != null) {
                var textBoxPeer = CreatePeerForElement(textBox);
                if (textBoxPeer != null) {
                    if (children == null)
                        children = new List<AutomationPeer>();
                    children.Insert(0, textBoxPeer);
                }
            }

            return children;
        }

        string IValueProvider.Value => ((DropDownTextBox)Owner).Text;

        void IValueProvider.SetValue(string val)
        {
            if (val == null)
                throw new ArgumentNullException(nameof(val));
            var owner = (DropDownTextBox)Owner;
            if (!owner.IsEnabled)
                throw new ElementNotEnabledException();
            owner.SetCurrentValue(DropDownTextBox.TextProperty, val);
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                var owner = (DropDownTextBox)Owner;
                if (owner.IsEnabled)
                    return owner.IsReadOnly;
                return true;
            }
        }

        void IExpandCollapseProvider.Expand()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();
            Owner.SetCurrentValue(DropDownTextBox.IsDropDownOpenProperty, Boxed.True);
        }

        void IExpandCollapseProvider.Collapse()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();
            Owner.SetCurrentValue(DropDownTextBox.IsDropDownOpenProperty, Boxed.True);
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                if (((DropDownTextBox)Owner).IsDropDownOpen)
                    return ExpandCollapseState.Expanded;
                return ExpandCollapseState.Collapsed;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void RaiseValuePropertyChangedEvent(string oldValue, string newValue)
        {
            if (oldValue == newValue)
                return;
            RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }
    }
}
