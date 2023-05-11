using System.Collections.Generic;
using UnityEngine;
using UXF;

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

    // handle left handed participants
    session.participantDetails.TryGetValue("ppt_right_handed", out object pptRightHanded);

    if (session.settings.ContainsKey("per_block_hand") && !(bool)pptRightHanded)
    {
      foreach (Block block in session.blocks)
      {
        // set the per_block_hand to be the opposite of what it was
        if (block.settings.GetString("per_block_hand") == "r")
        {
          block.settings.SetValue("per_block_hand", "l");
        }
        else
        {
          block.settings.SetValue("per_block_hand", "r");
        }
      }
    }

    GetComponent<ExperimentController>().Init(session);
  }
}
