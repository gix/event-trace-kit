namespace EventTraceKit.Dev14
{
    using System;
    using Microsoft.VisualStudio.Shell;

    internal static class CommonControlBrushes
    {
        private static readonly Guid Category = new Guid("C01072A1-A915-4ABF-89B7-E2F9E8EC4C7F");

        private static ThemeResourceKey buttonBackgroundKey;
        private static ThemeResourceKey buttonForegroundKey;
        private static ThemeResourceKey buttonBorderBackgroundKey;
        private static ThemeResourceKey buttonDefaultBackgroundKey;
        private static ThemeResourceKey buttonDefaultForegroundKey;
        private static ThemeResourceKey buttonDisabledBackgroundKey;
        private static ThemeResourceKey buttonDisabledForegroundKey;
        private static ThemeResourceKey buttonFocusedBackgroundKey;
        private static ThemeResourceKey buttonFocusedForegroundKey;
        private static ThemeResourceKey buttonHoverBackgroundKey;
        private static ThemeResourceKey buttonHoverForegroundKey;
        private static ThemeResourceKey buttonPressedBackgroundKey;
        private static ThemeResourceKey buttonPressedForegroundKey;
        private static ThemeResourceKey buttonBorderDefaultBackgroundKey;
        private static ThemeResourceKey buttonBorderDisabledBackgroundKey;
        private static ThemeResourceKey buttonBorderFocusedBackgroundKey;
        private static ThemeResourceKey buttonBorderHoverBackgroundKey;
        private static ThemeResourceKey buttonBorderPressedBackgroundKey;

        private static ThemeResourceKey checkBoxBackgroundBackgroundKey;
        private static ThemeResourceKey checkBoxBackgroundDisabledBackgroundKey;
        private static ThemeResourceKey checkBoxBackgroundFocusedBackgroundKey;
        private static ThemeResourceKey checkBoxBackgroundHoverBackgroundKey;
        private static ThemeResourceKey checkBoxBackgroundPressedBackgroundKey;
        private static ThemeResourceKey checkBoxBorderBackgroundKey;
        private static ThemeResourceKey checkBoxBorderDisabledBackgroundKey;
        private static ThemeResourceKey checkBoxBorderFocusedBackgroundKey;
        private static ThemeResourceKey checkBoxBorderHoverBackgroundKey;
        private static ThemeResourceKey checkBoxBorderPressedBackgroundKey;
        private static ThemeResourceKey checkBoxGlyphBackgroundKey;
        private static ThemeResourceKey checkBoxGlyphDisabledBackgroundKey;
        private static ThemeResourceKey checkBoxGlyphFocusedBackgroundKey;
        private static ThemeResourceKey checkBoxGlyphHoverBackgroundKey;
        private static ThemeResourceKey checkBoxGlyphPressedBackgroundKey;
        private static ThemeResourceKey checkBoxTextBackgroundKey;
        private static ThemeResourceKey checkBoxTextDisabledBackgroundKey;
        private static ThemeResourceKey checkBoxTextFocusedBackgroundKey;
        private static ThemeResourceKey checkBoxTextHoverBackgroundKey;
        private static ThemeResourceKey checkBoxTextPressedBackgroundKey;

        private static ThemeResourceKey comboBoxBackgroundBackgroundKey;
        private static ThemeResourceKey comboBoxBackgroundDisabledBackgroundKey;
        private static ThemeResourceKey comboBoxBackgroundFocusedBackgroundKey;
        private static ThemeResourceKey comboBoxBackgroundHoverBackgroundKey;
        private static ThemeResourceKey comboBoxBackgroundPressedBackgroundKey;
        private static ThemeResourceKey comboBoxBorderBackgroundKey;
        private static ThemeResourceKey comboBoxBorderDisabledBackgroundKey;
        private static ThemeResourceKey comboBoxBorderFocusedBackgroundKey;
        private static ThemeResourceKey comboBoxBorderHoverBackgroundKey;
        private static ThemeResourceKey comboBoxBorderPressedBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphBackgroundBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphBackgroundDisabledBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphBackgroundFocusedBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphBackgroundHoverBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphBackgroundPressedBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphDisabledBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphFocusedBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphHoverBackgroundKey;
        private static ThemeResourceKey comboBoxGlyphPressedBackgroundKey;
        private static ThemeResourceKey comboBoxListBackgroundBackgroundKey;
        private static ThemeResourceKey comboBoxListBackgroundShadowBackgroundKey;
        private static ThemeResourceKey comboBoxListBorderBackgroundKey;
        private static ThemeResourceKey comboBoxListItemBackgroundHoverBackgroundKey;
        private static ThemeResourceKey comboBoxListItemBorderHoverBackgroundKey;
        private static ThemeResourceKey comboBoxListItemTextBackgroundKey;
        private static ThemeResourceKey comboBoxListItemTextHoverBackgroundKey;
        private static ThemeResourceKey comboBoxSelectionBackgroundKey;
        private static ThemeResourceKey comboBoxSeparatorBackgroundKey;
        private static ThemeResourceKey comboBoxSeparatorDisabledBackgroundKey;
        private static ThemeResourceKey comboBoxSeparatorFocusedBackgroundKey;
        private static ThemeResourceKey comboBoxSeparatorHoverBackgroundKey;
        private static ThemeResourceKey comboBoxSeparatorPressedBackgroundKey;
        private static ThemeResourceKey comboBoxTextBackgroundKey;
        private static ThemeResourceKey comboBoxTextDisabledBackgroundKey;
        private static ThemeResourceKey comboBoxTextFocusedBackgroundKey;
        private static ThemeResourceKey comboBoxTextHoverBackgroundKey;
        private static ThemeResourceKey comboBoxTextInputSelectionBackgroundKey;
        private static ThemeResourceKey comboBoxTextPressedBackgroundKey;
        private static ThemeResourceKey focusVisualBackgroundKey;
        private static ThemeResourceKey focusVisualForegroundKey;
        private static ThemeResourceKey innerTabActiveBackgroundBackgroundKey;
        private static ThemeResourceKey innerTabActiveBorderBackgroundKey;
        private static ThemeResourceKey innerTabActiveTextBackgroundKey;
        private static ThemeResourceKey innerTabInactiveBackgroundBackgroundKey;
        private static ThemeResourceKey innerTabInactiveBorderBackgroundKey;
        private static ThemeResourceKey innerTabInactiveHoverBackgroundBackgroundKey;
        private static ThemeResourceKey innerTabInactiveHoverBorderBackgroundKey;
        private static ThemeResourceKey innerTabInactiveHoverTextBackgroundKey;
        private static ThemeResourceKey innerTabInactiveTextBackgroundKey;
        private static ThemeResourceKey textBoxBackgroundBackgroundKey;
        private static ThemeResourceKey textBoxBackgroundDisabledBackgroundKey;
        private static ThemeResourceKey textBoxBackgroundFocusedBackgroundKey;
        private static ThemeResourceKey textBoxBorderBackgroundKey;
        private static ThemeResourceKey textBoxBorderDisabledBackgroundKey;
        private static ThemeResourceKey textBoxBorderFocusedBackgroundKey;
        private static ThemeResourceKey textBoxTextBackgroundKey;
        private static ThemeResourceKey textBoxTextDisabledBackgroundKey;
        private static ThemeResourceKey textBoxTextFocusedBackgroundKey;

        public static ThemeResourceKey CheckBoxBackgroundDisabledBackgroundKey =>
            checkBoxBackgroundDisabledBackgroundKey ??
            (checkBoxBackgroundDisabledBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBackgroundDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxBackgroundDisabledBackgroundKey =>
            textBoxBackgroundDisabledBackgroundKey ??
            (textBoxBackgroundDisabledBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxBackgroundDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxGlyphBackgroundKey =>
            checkBoxGlyphBackgroundKey ??
            (checkBoxGlyphBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxGlyph", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxGlyphPressedBackgroundKey =>
            checkBoxGlyphPressedBackgroundKey ??
            (checkBoxGlyphPressedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxGlyphPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxListItemTextBackgroundKey =>
            comboBoxListItemTextBackgroundKey ??
            (comboBoxListItemTextBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxListItemText", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonBorderHoverBackgroundKey =>
            buttonBorderHoverBackgroundKey ??
            (buttonBorderHoverBackgroundKey = new ThemeResourceKey(
                Category, "ButtonBorderHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBorderHoverBackgroundKey =>
            comboBoxBorderHoverBackgroundKey ??
            (comboBoxBorderHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBorderHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonPressedBackgroundKey =>
            buttonPressedBackgroundKey ??
            (buttonPressedBackgroundKey = new ThemeResourceKey(
                Category, "ButtonPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonPressedTextKey =>
            buttonPressedForegroundKey ??
            (buttonPressedForegroundKey = new ThemeResourceKey(
                Category, "ButtonPressed", ThemeResourceKeyType.ForegroundBrush));

        public static ThemeResourceKey CheckBoxBorderHoverBackgroundKey =>
            checkBoxBorderHoverBackgroundKey ??
            (checkBoxBorderHoverBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBorderHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxBorderDisabledBackgroundKey =>
            textBoxBorderDisabledBackgroundKey ??
            (textBoxBorderDisabledBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxBorderDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxTextHoverBackgroundKey =>
            comboBoxTextHoverBackgroundKey ??
            (comboBoxTextHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxTextHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphHoverBackgroundKey =>
            comboBoxGlyphHoverBackgroundKey ??
            (comboBoxGlyphHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxTextInputSelectionBackgroundKey =>
            comboBoxTextInputSelectionBackgroundKey ??
            (comboBoxTextInputSelectionBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxTextInputSelection", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxSeparatorHoverBackgroundKey =>
            comboBoxSeparatorHoverBackgroundKey ??
            (comboBoxSeparatorHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxSeparatorHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonBorderFocusedBackgroundKey =>
            buttonBorderFocusedBackgroundKey ??
            (buttonBorderFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ButtonBorderFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabInactiveHoverTextBackgroundKey =>
            innerTabInactiveHoverTextBackgroundKey ??
            (innerTabInactiveHoverTextBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabInactiveHoverText", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxSeparatorFocusedBackgroundKey =>
            comboBoxSeparatorFocusedBackgroundKey ??
            (comboBoxSeparatorFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxSeparatorFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBorderPressedBackgroundKey =>
            comboBoxBorderPressedBackgroundKey ??
            (comboBoxBorderPressedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBorderPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxSeparatorBackgroundKey =>
            comboBoxSeparatorBackgroundKey ??
            (comboBoxSeparatorBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxSeparator", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxSelectionBackgroundKey =>
            comboBoxSelectionBackgroundKey ??
            (comboBoxSelectionBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxSelection", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBorderDisabledBackgroundKey =>
            comboBoxBorderDisabledBackgroundKey ??
            (comboBoxBorderDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBorderDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabInactiveTextBackgroundKey =>
            innerTabInactiveTextBackgroundKey ??
            (innerTabInactiveTextBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabInactiveText", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonBorderPressedBackgroundKey =>
            buttonBorderPressedBackgroundKey ??
            (buttonBorderPressedBackgroundKey = new ThemeResourceKey(
                Category, "ButtonBorderPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBackgroundFocusedBackgroundKey =>
            comboBoxBackgroundFocusedBackgroundKey ??
            (comboBoxBackgroundFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBackgroundFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonTextKey =>
            buttonForegroundKey ??
            (buttonForegroundKey = new ThemeResourceKey(
                Category, "Button", ThemeResourceKeyType.ForegroundBrush));

        public static ThemeResourceKey ButtonBackgroundKey =>
            buttonBackgroundKey ??
            (buttonBackgroundKey = new ThemeResourceKey(
                Category, "Button", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxBorderFocusedBackgroundKey =>
            textBoxBorderFocusedBackgroundKey ??
            (textBoxBorderFocusedBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxBorderFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxListItemBorderHoverBackgroundKey =>
            comboBoxListItemBorderHoverBackgroundKey ??
            (comboBoxListItemBorderHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxListItemBorderHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxBackgroundBackgroundKey =>
            textBoxBackgroundBackgroundKey ??
            (textBoxBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphBackgroundFocusedBackgroundKey =>
            comboBoxGlyphBackgroundFocusedBackgroundKey ??
            (comboBoxGlyphBackgroundFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphBackgroundFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBorderBackgroundKey =>
            checkBoxBorderBackgroundKey ??
            (checkBoxBorderBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBackgroundHoverBackgroundKey =>
            checkBoxBackgroundHoverBackgroundKey ??
            (checkBoxBackgroundHoverBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBackgroundHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBackgroundDisabledBackgroundKey =>
            comboBoxBackgroundDisabledBackgroundKey ??
            (comboBoxBackgroundDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBackgroundDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphBackgroundHoverBackgroundKey =>
            comboBoxGlyphBackgroundHoverBackgroundKey ??
            (comboBoxGlyphBackgroundHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphBackgroundHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxTextPressedBackgroundKey =>
            comboBoxTextPressedBackgroundKey ??
            (comboBoxTextPressedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxTextPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBackgroundBackgroundKey =>
            checkBoxBackgroundBackgroundKey ??
            (checkBoxBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabActiveTextBackgroundKey =>
            innerTabActiveTextBackgroundKey ??
            (innerTabActiveTextBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabActiveText", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBorderDisabledBackgroundKey =>
            checkBoxBorderDisabledBackgroundKey ??
            (checkBoxBorderDisabledBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBorderDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonDisabledBackgroundKey =>
            buttonDisabledBackgroundKey ??
            (buttonDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ButtonDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonDisabledTextKey =>
            buttonDisabledForegroundKey ??
            (buttonDisabledForegroundKey = new ThemeResourceKey(
                Category, "ButtonDisabled", ThemeResourceKeyType.ForegroundBrush));

        public static ThemeResourceKey InnerTabActiveBorderBackgroundKey =>
            innerTabActiveBorderBackgroundKey ??
            (innerTabActiveBorderBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabActiveBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey FocusVisualForegroundKey =>
            focusVisualForegroundKey ??
            (focusVisualForegroundKey = new ThemeResourceKey(
                Category, "FocusVisual", ThemeResourceKeyType.ForegroundBrush));

        public static ThemeResourceKey ComboBoxBorderBackgroundKey =>
            comboBoxBorderBackgroundKey ??
            (comboBoxBorderBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxTextBackgroundKey =>
            comboBoxTextBackgroundKey ??
            (comboBoxTextBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxText", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabInactiveBackgroundBackgroundKey =>
            innerTabInactiveBackgroundBackgroundKey ??
            (innerTabInactiveBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabInactiveBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxTextFocusedBackgroundKey =>
            comboBoxTextFocusedBackgroundKey ??
            (comboBoxTextFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxTextFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxTextBackgroundKey =>
            textBoxTextBackgroundKey ??
            (textBoxTextBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxText", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxGlyphFocusedBackgroundKey =>
            checkBoxGlyphFocusedBackgroundKey ??
            (checkBoxGlyphFocusedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxGlyphFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxTextDisabledBackgroundKey =>
            comboBoxTextDisabledBackgroundKey ??
            (comboBoxTextDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxTextDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxListItemTextHoverBackgroundKey =>
            comboBoxListItemTextHoverBackgroundKey ??
            (comboBoxListItemTextHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxListItemTextHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonHoverTextKey =>
            buttonHoverForegroundKey ??
            (buttonHoverForegroundKey = new ThemeResourceKey(
                Category, "ButtonHover", ThemeResourceKeyType.ForegroundBrush));

        public static ThemeResourceKey ButtonHoverBackgroundKey =>
            buttonHoverBackgroundKey ??
            (buttonHoverBackgroundKey = new ThemeResourceKey(
                Category, "ButtonHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphPressedBackgroundKey =>
            comboBoxGlyphPressedBackgroundKey ??
            (comboBoxGlyphPressedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxListBackgroundShadowBackgroundKey =>
            comboBoxListBackgroundShadowBackgroundKey ??
            (comboBoxListBackgroundShadowBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxListBackgroundShadow", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxSeparatorPressedBackgroundKey =>
            comboBoxSeparatorPressedBackgroundKey ??
            (comboBoxSeparatorPressedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxSeparatorPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBackgroundPressedBackgroundKey =>
            comboBoxBackgroundPressedBackgroundKey ??
            (comboBoxBackgroundPressedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBackgroundPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxTextPressedBackgroundKey =>
            checkBoxTextPressedBackgroundKey ??
            (checkBoxTextPressedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxTextPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxBackgroundFocusedBackgroundKey =>
            textBoxBackgroundFocusedBackgroundKey ??
            (textBoxBackgroundFocusedBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxBackgroundFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBorderPressedBackgroundKey =>
            checkBoxBorderPressedBackgroundKey ??
            (checkBoxBorderPressedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBorderPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxTextFocusedBackgroundKey =>
            textBoxTextFocusedBackgroundKey ??
            (textBoxTextFocusedBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxTextFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabInactiveHoverBorderBackgroundKey =>
            innerTabInactiveHoverBorderBackgroundKey ??
            (innerTabInactiveHoverBorderBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabInactiveHoverBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxGlyphHoverBackgroundKey =>
            checkBoxGlyphHoverBackgroundKey ??
            (checkBoxGlyphHoverBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxGlyphHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBackgroundFocusedBackgroundKey =>
            checkBoxBackgroundFocusedBackgroundKey ??
            (checkBoxBackgroundFocusedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBackgroundFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphFocusedBackgroundKey =>
            comboBoxGlyphFocusedBackgroundKey ??
            (comboBoxGlyphFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonDefaultTextKey =>
            buttonDefaultForegroundKey ??
            (buttonDefaultForegroundKey = new ThemeResourceKey(
                Category, "ButtonDefault", ThemeResourceKeyType.ForegroundBrush));

        public static ThemeResourceKey ButtonDefaultBackgroundKey =>
            buttonDefaultBackgroundKey ??
            (buttonDefaultBackgroundKey = new ThemeResourceKey(
                Category, "ButtonDefault", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxBorderBackgroundKey =>
            textBoxBorderBackgroundKey ??
            (textBoxBorderBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBackgroundBackgroundKey =>
            comboBoxBackgroundBackgroundKey ??
            (comboBoxBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBackgroundPressedBackgroundKey =>
            checkBoxBackgroundPressedBackgroundKey ??
            (checkBoxBackgroundPressedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBackgroundPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphBackgroundKey =>
            comboBoxGlyphBackgroundKey ??
            (comboBoxGlyphBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyph", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxTextBackgroundKey =>
            checkBoxTextBackgroundKey ??
            (checkBoxTextBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxText", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxListBackgroundBackgroundKey =>
            comboBoxListBackgroundBackgroundKey ??
            (comboBoxListBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxListBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxListBorderBackgroundKey =>
            comboBoxListBorderBackgroundKey ??
            (comboBoxListBorderBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxListBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxBorderFocusedBackgroundKey =>
            checkBoxBorderFocusedBackgroundKey ??
            (checkBoxBorderFocusedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxBorderFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonFocusedBackgroundKey =>
            buttonFocusedBackgroundKey ??
            (buttonFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ButtonFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonFocusedTextKey =>
            buttonFocusedForegroundKey ??
            (buttonFocusedForegroundKey = new ThemeResourceKey(
                Category, "ButtonFocused", ThemeResourceKeyType.ForegroundBrush));

        public static ThemeResourceKey FocusVisualBackgroundKey =>
            focusVisualBackgroundKey ??
            (focusVisualBackgroundKey = new ThemeResourceKey(
                Category, "FocusVisual", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabInactiveBorderBackgroundKey =>
            innerTabInactiveBorderBackgroundKey ??
            (innerTabInactiveBorderBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabInactiveBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxTextDisabledBackgroundKey =>
            checkBoxTextDisabledBackgroundKey ??
            (checkBoxTextDisabledBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxTextDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxTextHoverBackgroundKey =>
            checkBoxTextHoverBackgroundKey ??
            (checkBoxTextHoverBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxTextHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabInactiveHoverBackgroundBackgroundKey =>
            innerTabInactiveHoverBackgroundBackgroundKey ??
            (innerTabInactiveHoverBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabInactiveHoverBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey TextBoxTextDisabledBackgroundKey =>
            textBoxTextDisabledBackgroundKey ??
            (textBoxTextDisabledBackgroundKey = new ThemeResourceKey(
                Category, "TextBoxTextDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonBorderDisabledBackgroundKey =>
            buttonBorderDisabledBackgroundKey ??
            (buttonBorderDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ButtonBorderDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBackgroundHoverBackgroundKey =>
            comboBoxBackgroundHoverBackgroundKey ??
            (comboBoxBackgroundHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBackgroundHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphBackgroundPressedBackgroundKey =>
            comboBoxGlyphBackgroundPressedBackgroundKey ??
            (comboBoxGlyphBackgroundPressedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphBackgroundPressed", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonBorderBackgroundKey =>
            buttonBorderBackgroundKey ??
            (buttonBorderBackgroundKey = new ThemeResourceKey(
                Category, "ButtonBorder", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxGlyphDisabledBackgroundKey =>
            checkBoxGlyphDisabledBackgroundKey ??
            (checkBoxGlyphDisabledBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxGlyphDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphBackgroundDisabledBackgroundKey =>
            comboBoxGlyphBackgroundDisabledBackgroundKey ??
            (comboBoxGlyphBackgroundDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphBackgroundDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphBackgroundBackgroundKey =>
            comboBoxGlyphBackgroundBackgroundKey ??
            (comboBoxGlyphBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxBorderFocusedBackgroundKey =>
            comboBoxBorderFocusedBackgroundKey ??
            (comboBoxBorderFocusedBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxBorderFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxListItemBackgroundHoverBackgroundKey =>
            comboBoxListItemBackgroundHoverBackgroundKey ??
            (comboBoxListItemBackgroundHoverBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxListItemBackgroundHover", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ButtonBorderDefaultBackgroundKey =>
            buttonBorderDefaultBackgroundKey ??
            (buttonBorderDefaultBackgroundKey = new ThemeResourceKey(
                Category, "ButtonBorderDefault", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey InnerTabActiveBackgroundBackgroundKey =>
            innerTabActiveBackgroundBackgroundKey ??
            (innerTabActiveBackgroundBackgroundKey = new ThemeResourceKey(
                Category, "InnerTabActiveBackground", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxGlyphDisabledBackgroundKey =>
            comboBoxGlyphDisabledBackgroundKey ??
            (comboBoxGlyphDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxGlyphDisabled", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey CheckBoxTextFocusedBackgroundKey =>
            checkBoxTextFocusedBackgroundKey ??
            (checkBoxTextFocusedBackgroundKey = new ThemeResourceKey(
                Category, "CheckBoxTextFocused", ThemeResourceKeyType.BackgroundBrush));

        public static ThemeResourceKey ComboBoxSeparatorDisabledBackgroundKey =>
            comboBoxSeparatorDisabledBackgroundKey ??
            (comboBoxSeparatorDisabledBackgroundKey = new ThemeResourceKey(
                Category, "ComboBoxSeparatorDisabled", ThemeResourceKeyType.BackgroundBrush));
    }
}
