namespace EventTraceKit.VsExtension.Tests.Filtering
{
    using System.Linq;
    using EventTraceKit.VsExtension.Filtering;
    using Xunit;

    public class FilterDialogViewModelTest
    {
        [Fact]
        public void EmptyModel()
        {
            var model = new FilterDialogViewModel();

            Assert.Empty(model.Conditions);
            Assert.False(model.AddCommand.CanExecute(null));
            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AcceptCommand.CanExecute(null));
            Assert.False(model.ApplyCommand.CanExecute(null));
        }

        [Fact]
        public void SimpleCondition_Add()
        {
            var model = new FilterDialogViewModel();
            model.SelectedProperty = model.Properties.Single(x => x.Name == "Id");
            model.SelectedRelation = model.Relations.Single(x => x.DisplayName == "==");
            model.TargetValue.RawValue = (ushort)23;
            model.SelectedAction = FilterConditionAction.Exclude;

            Assert.True(model.AddCommand.CanExecute(null));
            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AcceptCommand.CanExecute(null));

            model.AddCommand.Execute(null);

            Assert.Single(model.Conditions);
            Assert.Equal("Id == 23", model.Conditions[0].DisplayName);
            Assert.True(model.Conditions[0].IsEnabled);
            Assert.Equal(FilterConditionAction.Exclude, model.Conditions[0].Action);
            Assert.True(model.AddCommand.CanExecute(null));
            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AcceptCommand.CanExecute(null));
        }

        [Fact]
        public void SimpleCondition_Remove()
        {
            var model = new FilterDialogViewModel();
            var idProperty = model.Properties.Single(x => x.Name == "Id");
            var relation = idProperty.Relations.First();
            model.Conditions.Add(new SimpleFilterConditionViewModel(
                idProperty,
                new TraceLogFilterCondition(
                    idProperty.Expression, true, relation.Kind,
                    FilterConditionAction.Exclude, (ushort)23)));
            model.SelectedCondition = model.Conditions[0];
            model.AdvancedMode = true;

            Assert.True(model.RemoveCommand.CanExecute(null));

            model.RemoveCommand.Execute(null);

            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AddCommand.CanExecute(null));
            Assert.Null(model.SelectedCondition);
            Assert.Equal(idProperty, model.SelectedProperty);
            Assert.Equal(relation, model.SelectedRelation);
            Assert.Equal((ushort)23, model.TargetValue.RawValue);
            Assert.False(model.AdvancedMode);
            Assert.Equal(FilterConditionAction.Exclude, model.SelectedAction);
        }

        [Fact]
        public void AdvancedCondition_Add()
        {
            var model = new FilterDialogViewModel();
            model.AdvancedMode = true;
            model.Expression = "Id == 23";
            model.SelectedAction = FilterConditionAction.Exclude;

            Assert.True(model.AddCommand.CanExecute(null));
            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AcceptCommand.CanExecute(null));

            model.AddCommand.Execute(null);

            Assert.Single(model.Conditions);
            Assert.Equal("Id == 23", model.Conditions[0].DisplayName);
            Assert.True(model.Conditions[0].IsEnabled);
            Assert.Equal(FilterConditionAction.Exclude, model.Conditions[0].Action);
            Assert.True(model.AddCommand.CanExecute(null));
            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AcceptCommand.CanExecute(null));
        }

        [Fact]
        public void AdvancedCondition_CannotAddInvalid()
        {
            var model = new FilterDialogViewModel();
            model.AdvancedMode = true;
            model.Expression = "Id == ProviderId";
            model.SelectedAction = FilterConditionAction.Exclude;

            Assert.False(model.AddCommand.CanExecute(null));
            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AcceptCommand.CanExecute(null));
        }

        [Fact]
        public void AdvancedCondition_Remove()
        {
            var model = new FilterDialogViewModel();
            model.Conditions.Add(new AdvancedFilterConditionViewModel(
                new TraceLogFilterCondition("Id == 23", true, FilterConditionAction.Exclude)));
            model.SelectedCondition = model.Conditions[0];

            Assert.True(model.RemoveCommand.CanExecute(null));

            model.RemoveCommand.Execute(null);

            Assert.False(model.RemoveCommand.CanExecute(null));
            Assert.True(model.AddCommand.CanExecute(null));
            Assert.Null(model.SelectedCondition);
            Assert.Equal("Id == 23", model.Expression);
            Assert.True(model.AdvancedMode);
            Assert.Equal(FilterConditionAction.Exclude, model.SelectedAction);
        }
    }
}
