﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class KMBossModule : MonoBehaviour
{
    public string[] GetIgnoredModules(KMBombModule module, string[] @default = null)
    {
        return GetIgnoredModules(module.ModuleDisplayName, @default);
    }

    public string[] GetIgnoredModules(string moduleDisplayName, string[] @default = null)
    {
        if (Application.isEditor)
            return @default ?? new string[0];

        var bossModuleManagerAPIGameObject = GameObject.Find("BossModuleManager");
        if (bossModuleManagerAPIGameObject == null) // Boss Module Manager is not installed
        {
            Debug.LogFormat(@"[KMBossModule] Boss Module Manager is not installed.");
            return @default ?? new string[0];
        }

        var bossModuleManagerAPI = bossModuleManagerAPIGameObject.GetComponent<IDictionary<string, object>>();
        if (bossModuleManagerAPI == null || !bossModuleManagerAPI.ContainsKey("GetIgnoredModules"))
        {
            Debug.LogFormat(@"[KMBossModule] Boss Module Manager does not have a module name list on record for “{0}”.", moduleDisplayName);
            return @default ?? new string[0];
        }

        var list = ((Func<string, string[]>) bossModuleManagerAPI["GetIgnoredModules"])(moduleDisplayName);
        Debug.LogFormat(@"[KMBossModule] Boss Module Manager returned a module name list for “{0}”: {1}", moduleDisplayName, list == null ? "<null>" : list.Join(", "));
        return list ?? @default ?? new string[0];
    }
    public string[] GetIgnoredModuleIDs(KMBombModule module, string[] @default = null)
    {
        return GetIgnoredModuleIDs(module.ModuleDisplayName, @default);
    }

    public string[] GetIgnoredModuleIDs(string moduleDisplayName, string[] @default = null)
    {
        if (Application.isEditor)
            return @default ?? new string[0];

        var bossModuleManagerAPIGameObject = GameObject.Find("BossModuleManager");
        if (bossModuleManagerAPIGameObject == null) // Boss Module Manager is not installed
        {
            Debug.LogFormat(@"[KMBossModule] Boss Module Manager is not installed.");
            return @default ?? new string[0];
        }

        var bossModuleManagerAPI = bossModuleManagerAPIGameObject.GetComponent<IDictionary<string, object>>();
        if (bossModuleManagerAPI == null || !bossModuleManagerAPI.ContainsKey("GetIgnoredModuleIDs"))
        {
            Debug.LogFormat(@"[KMBossModule] Boss Module Manager does not have an ID list on record for “{0}”.", moduleDisplayName);
            return @default ?? new string[0];
        }

        var list = ((Func<string, string[]>)bossModuleManagerAPI["GetIgnoredModuleIDs"])(moduleDisplayName);
        Debug.LogFormat(@"[KMBossModule] Boss Module Manager returned an ID list for “{0}”: {1}", moduleDisplayName, list == null ? "<null>" : list.Join(", "));
        return list ?? @default ?? new string[0];
    }
}
