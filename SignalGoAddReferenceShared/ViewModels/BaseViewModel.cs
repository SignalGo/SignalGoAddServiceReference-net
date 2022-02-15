using System;
using System.Collections.Generic;

namespace SignalGoAddReferenceShared.ViewModels
{
    public class BaseViewModel : MvvmGo.ViewModels.BaseViewModel
    {
        public BaseViewModel()
        {
            foreach (var property in GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (property.CanWrite && property.CanRead)
                    property.SetValue(this, property.GetValue(this));
            }
        }

        Action<bool> _Changed;
        bool _IsValidate;

        public Action<bool> Changed
        {
            get => _Changed;
            set
            {
                _Changed = value;
            }
        }

        public bool IsValidate
        {
            get => _IsValidate;
            set
            {
                _IsValidate = value;
                Changed?.Invoke(_IsValidate);
            }
        }

        Dictionary<string, Func<bool>> Validations { get; set; } = new Dictionary<string, Func<bool>>();

        public void OnPropertyChanged(string name, Func<bool> checkIsValidate)
        {
            if (!Validations.TryGetValue(name, out _))
                Validations.Add(name, checkIsValidate);
            CheckValidations();
        }

        public void CheckValidations()
        {
            bool isValidate = true;
            foreach (var property in Validations)
            {
                if (!property.Value())
                {
                    isValidate = false;
                    break;
                }
            }
            IsValidate = isValidate;
        }
    }
}
