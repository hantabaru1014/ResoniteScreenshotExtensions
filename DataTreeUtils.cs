using Elements.Core;
using FrooxEngine;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using HarmonyLib;

namespace ResoniteScreenshotExtensions;

public static class DataTreeUtils
{
    public static SavedGraph SaveComponent(IComponent component)
    {
        var engine = component.Slot.Engine;
        var tree = new DataTreeDictionary();
        var control = new SaveControl(component.Slot, new ReferenceTranslator(), null);
        control.SaveNonPersistent = true;
        tree.Add("VersionNumber", engine.VersionString);
        tree.Add("FeatureFlags", control.StoreFeatureFlags(engine));
        var typeVersions = new DataTreeDictionary();
        tree.Add("TypeVersions", typeVersions);
        tree.Add("Component", component.Save(control));
        control.StoreTypeVersions(typeVersions);
        return new SavedGraph(tree);
    }

    public static void LoadComponent(IComponent component, SavedGraph graph)
    {
        var dict = graph.Root;
        var control = new LoadControl(component.Slot.World, new ReferenceTranslator(), default);
        control.TryLoadVersion(dict);
        var flags = dict.TryGetDictionary("FeatureFlags");
        if (flags != null)
        {
            control.LoadFeatureFlags(flags);
        }
        control.InitializeLoaders();
        var typeVersions = dict.TryGetDictionary("TypeVersions");
        if (typeVersions != null)
        {
            control.LoadTypeVersions(typeVersions);
        }
        var data = dict.TryGetNode("Component");
        if (data != null)
        {
            component.Load(data, control);
        }
        AccessTools.Method(typeof(LoadControl), "FinishLoad").Invoke(control, null);
    }

    public static string ToJson(SavedGraph graph)
    {
        var sb = new StringBuilder();
        var writer = new JsonTextWriter(new StringWriter(sb));
        AccessTools.Method(typeof(DataTreeConverter), "Write").Invoke(null, new object[] { graph.Root, writer });
        return sb.ToString();
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