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
        List<int> perBlockN = session.settings.GetIntList("per_block_n");
        string experimentMode = session.settings.GetString("experiment_mode");

        for (int i = 0; i < perBlockN.Count; i++)
        {
            session.CreateBlock(perBlockN[i]);
            session.blocks[i].settings.SetValue("experiment_mode", experimentMode);

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
