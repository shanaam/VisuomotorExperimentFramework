using System.Collections.Generic;
using UnityEngine;
using UXF;

/*
 * File: ExperimentGenerator.cs
 * License: York University (c) 2020
 * Author: Mark Voong
 * Desc: Loads JSON and generates blocks for experiment
 */

public class ExperimentGenerator : MonoBehaviour
{
    public void GenerateBlocks(Session session)
    {
        var keys = session.settings.Keys;
        List<int> per_block_n = session.settings.GetIntList("per_block_n");
        string experiment_mode = session.settings.GetString("experiment_mode");

        for (int i = 0; i < per_block_n.Count; i++)
        {
            session.CreateBlock(per_block_n[i]);
            session.blocks[i].settings.SetValue("experiment_mode", experiment_mode);

            foreach (string key in keys)
            {
                if (key != "per_block_n" && key.StartsWith("per_"))
                {
                    session.blocks[i].settings.SetValue(key, session.settings.GetObjectList(key)[i]);        
                }
            }
        }

        GetComponent<ExperimentController>().Init(session);
    }
}
