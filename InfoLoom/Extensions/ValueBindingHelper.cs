// <copyright file="ValueBindingHelper.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace InfoLoomTwo.Extensions
{
    using System;
    using Colossal.UI.Binding;

    public class ValueBindingHelper<T>
    {
        private readonly Action<T>? _updateCallBack;
        private T? valueToUpdate;
        private bool dirty;

        public ValueBinding<T?> Binding { get; }

        public T? Value
        {
            get => dirty ? valueToUpdate : Binding.value;
            set
            {
                dirty = true;
                valueToUpdate = value;
            }
        }

        public ValueBindingHelper(ValueBinding<T?> binding, Action<T>? updateCallBack = null)
        {
            Binding = binding;
            _updateCallBack = updateCallBack;
        }

        public void ForceUpdate()
        {
            if (dirty)
            {
                Binding.Update(valueToUpdate);

                dirty = false;
            }
        }

        public void UpdateCallback(T value)
        {
            Value = value;

            _updateCallBack?.Invoke(value);
        }

        public static implicit operator T?(ValueBindingHelper<T> helper)
        {
            return helper.Value;
        }
    }
}
