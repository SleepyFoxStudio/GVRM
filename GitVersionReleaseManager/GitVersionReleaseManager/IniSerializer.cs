using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IniParser;
using IniParser.Model;

namespace GitVersionReleaseManager
{
    public class SectionAttribute : Attribute
    {
        public string SectionName { get; }

        public SectionAttribute(string sectionName)
        {
            SectionName = sectionName;
        }
    }

    public class IniSerializer<T> where T : new()
    {
        public string WriteToString(T data)
        {
            var iniData = WriteToIniData(data);
            return iniData.ToString();
        }

        public void WriteToFile(T data, string filePath)
        {
            var iniWriter = new FileIniDataParser();
            var iniData = WriteToIniData(data);

            iniWriter.WriteFile(filePath, iniData);
        }

        private static IniData WriteToIniData(T data)
        {
            var iniData = new IniParser.Model.IniData();

            foreach (var property in typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var sectionAttribute = property.GetCustomAttribute<SectionAttribute>();
                var sectionName = sectionAttribute?.SectionName ?? "default";

                var propertyValue = property.GetValue(data);

                var valueString = propertyValue?.ToString() ?? "";

                iniData.Sections.AddSection(sectionName);
                iniData[sectionName][property.Name] = valueString;
            }
            return iniData;
        }

        public T ReadFromString(string iniDataString)
        {
            var iniParser = new IniParser.Parser.IniDataParser();
            var iniData = iniParser.Parse(iniDataString);

            return ReadFromIniData(iniData);
        }

        public T ReadFromFile(string filePath)
        {
            var iniParser = new IniParser.FileIniDataParser();
            var iniData = iniParser.ReadFile(filePath);

            return ReadFromIniData(iniData);
        }

        private static T ReadFromIniData(IniData iniData)
        {
            var data = new T();
            var propDict = new Dictionary<string, List<PropertyInfo>>();

            foreach (var property in typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var sectionAttribute = property.GetCustomAttribute<SectionAttribute>();
                var sectionName = sectionAttribute?.SectionName ?? "default";

                if (!propDict.ContainsKey(sectionName))
                {
                    propDict[sectionName] = new List<PropertyInfo>();
                }

                propDict[sectionName].Add(property);
            }

            foreach (var keyData in iniData.Sections.SelectMany(
                s => s.Keys.Select(k => new {Section = s.SectionName, Key = k.KeyName, Value = k.Value})))
            {
                if (!propDict.ContainsKey(keyData.Section))
                    continue;

                var propertyInfo = propDict[keyData.Section].FirstOrDefault(p => p.Name == keyData.Key);
                if (propertyInfo == null)
                    continue;

                object typedKeyData = Convert.ChangeType(keyData.Value, propertyInfo.PropertyType);

                propertyInfo.SetValue(data, typedKeyData);
            }

            return data;
        }
    }
}