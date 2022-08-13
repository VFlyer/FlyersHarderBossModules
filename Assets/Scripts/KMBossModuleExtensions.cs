using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KMBossModuleExtensions : KMBossModule {

    public string[] GetAttachedIgnoredModuleIDs(KMBombModule modSelf, string[] @default = null)
    {
        string[] modNamesIgnored = GetIgnoredModules(modSelf, @default);
        // Redirect to KM Boss Module for standard boss module handling. 
        
        if (Application.isEditor)
        {
            return @default ?? new string[0];
        }
        

        KMBomb bombAttached = modSelf.gameObject.GetComponentInParent<KMBomb>();
        if (bombAttached == null)
        {
            Debug.LogFormat("[KMBossModuleExtensions] Unable to grab ignored mod IDs for “{0}” because KMBomb does not exist.", modSelf.ModuleDisplayName);
            return @default ?? new string[0];
        }
        KMBombModule[] allSolvables = bombAttached.gameObject.GetComponentsInChildren<KMBombModule>();
        if (allSolvables == null)
        {
            Debug.LogFormat("[KMBossModuleExtensions] Unable to grab ignored mod IDs for “{0}” because of detecting no solvable modules.", modSelf.ModuleDisplayName);
            return @default ?? new string[0];
        }
        string[] output = allSolvables.Where(a => modNamesIgnored.Contains(a.ModuleDisplayName)).Select(a => a.ModuleType).Distinct().ToArray();
        Debug.LogFormat("[KMBossModuleExtensions] Successfully grabbed ALL ignored module ids from the given bomb for “{0}”. Returning this: {1}", modSelf.ModuleDisplayName, output == null || !output.Any() ? "<null>" : output.Join(", "));
        return output;


    }

}
