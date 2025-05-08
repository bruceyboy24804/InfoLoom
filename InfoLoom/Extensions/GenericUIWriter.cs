﻿// <copyright file="GenericUIWriter.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace InfoLoomTwo.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Colossal.Reflection;
    using Colossal.UI.Binding;
    using Unity.Entities;
    using UnityEngine;

    public class GenericUIWriter<T> : IWriter<T>
    {
        public void Write(IJsonWriter writer, T value)
        {
            WriteGeneric(writer, value);
        }

        private static void WriteObject(IJsonWriter writer, Type type, object obj)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            writer.TypeBegin(type.FullName);

            foreach (var propertyInfo in properties.Where(x => !x.HasAttribute<WriterIgnoreAttribute>()))
            {
                writer.PropertyName(propertyInfo.Name);
                WriteGeneric(writer, propertyInfo.GetValue(obj));
            }

            foreach (var fieldInfo in fields.Where(x => !x.HasAttribute<WriterIgnoreAttribute>()))
            {
                writer.PropertyName(fieldInfo.Name);
                WriteGeneric(writer, fieldInfo.GetValue(obj));
            }

            writer.TypeEnd();
        }

        private static void WriteGeneric(IJsonWriter writer, object? obj)
        {
            if (obj == null)
            {
                writer.WriteNull();
                return;
            }

            if (obj is IJsonWritable jsonWritable)
            {
                jsonWritable.Write(writer);
                return;
            }

            if (obj is int @int)
            {
                writer.Write(@int);
                return;
            }

            if (obj is bool @bool)
            {
                writer.Write(@bool);
                return;
            }

            if (obj is uint @uint)
            {
                writer.Write(@uint);
                return;
            }

            if (obj is float @float)
            {
                writer.Write(@float);
                return;
            }

            if (obj is double @double)
            {
                writer.Write(@double);
                return;
            }

            if (obj is string @string)
            {
                writer.Write(@string);
                return;
            }

            if (obj is Enum @enum)
            {
                writer.Write(Convert.ToInt32(@enum));
                return;
            }

            if (obj is Entity entity)
            {
                writer.Write(entity);
                return;
            }

            if (obj is Color color)
            {
                writer.Write(color);
                return;
            }

            if (obj is Array array)
            {
                WriteArray(writer, array);
                return;
            }

            if (obj is IEnumerable objects)
            {
                WriteEnumerable(writer, objects);
                return;
            }

            WriteObject(writer, obj.GetType(), obj);
        }

        private static void WriteArray(IJsonWriter writer, Array array)
        {
            writer.ArrayBegin(array.Length);

            for (var i = 0; i < array.Length; i++)
            {
                WriteGeneric(writer, array.GetValue(i));
            }

            writer.ArrayEnd();
        }

        private static void WriteEnumerable(IJsonWriter writer, object? obj)
        {
            if (obj is not IEnumerable enumerable)
            {
                writer.WriteEmptyArray();
                return;
            }

            var list = new List<object>();

            foreach (var item in enumerable)
            {
                list.Add(item);
            }

            writer.ArrayBegin(list.Count);

            foreach (var item in list)
            {
                WriteGeneric(writer, item);
            }

            writer.ArrayEnd();
        }
    }

    public class WriterIgnoreAttribute : Attribute { }
}