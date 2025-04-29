using Elements.Core;
using FrooxEngine;
using Newtonsoft.Json;
using System.IO;
using HarmonyLib;

namespace ResoniteScreenshotExtensions;

public static class DataTreeUtils
{
    public static void LoadComponent(IComponent component, SavedGraph graph)
    {
        var dict = graph.Root;
        var control = new LoadControl(component.Slot.World, new ReferenceTranslator(), default, null);
        control.TryLoadVersion(dict);
        var flags = dict.TryGetDictionary("FeatureFlags");
        if (flags != null)
        {
            control.LoadFeatureFlags(flags);
        }
        control.InitializeLoaders();
        var typeList = dict.TryGetList("Types");
        var typeVersions = dict.TryGetDictionary("TypeVersions");
        if (typeVersions != null)
        {
            control.LoadTypeData(typeList, typeVersions);
        }
        var data = dict.TryGetNode("Component");
        if (data != null)
        {
            component.Load(data, control);
        }
        AccessTools.Method(typeof(LoadControl), "FinishLoad").Invoke(control, null);
    }

    public static SavedGraph? ToSavedGraph(string json)
    {
        var sr = new StringReader(json);
        var reader = new JsonTextReader(sr);
        var node = (DataTreeNode)AccessTools.Method(typeof(DataTreeConverter), "Read").Invoke(null, new object[] { reader });
        if (node != null)
        {
            return new SavedGraph((DataTreeDictionary)node);
        }
        else
        {
            return null;
        }
    }
}
